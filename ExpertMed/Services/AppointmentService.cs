﻿using ExpertMed.Models;
using iText.Commons.Actions.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Numerics;

namespace ExpertMed.Services
{
    public class AppointmentService
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AppointmentService> _logger;
        private readonly DbExpertmedContext _dbContext;

        public AppointmentService(IHttpContextAccessor httpContextAccessor, ILogger<AppointmentService> logger, DbExpertmedContext dbContext)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public class AvailableHour
        {
            public TimeSpan AvailableTime { get; set; }
        }


        public async Task<List<Appointment>> GetAllAppointmentAsync(int userProfile, int appointmentStatus, int? userId = null)
        {
            try
            {
                var parameters = new[]
                {
            new SqlParameter("@UserProfile", userProfile),
            new SqlParameter("@UserID", userId ?? (object)DBNull.Value),
            new SqlParameter("@AppointmentStatus", appointmentStatus)
        };

                var appointments = await _dbContext.Appointments
                    .FromSqlRaw("EXEC sp_ListAllAppointment @UserProfile, @UserID, @AppointmentStatus", parameters)
                    .ToListAsync();

                return appointments;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Error al ejecutar el procedimiento almacenado en la base de datos.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las citas.");
                throw;
            }
        }


        /// <summary>
        /// Obtener horas disponibles por medico
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="date"></param>
        /// <param name="doctorUserId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<string> GetAvailableHours(int userId, DateTime date, int? doctorUserId = null)
        {
            List<string> availableHours = new List<string>();

            using (SqlConnection conn = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                try
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_GetAvailableHours", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Parámetro de usuario (asistente o médico)
                        cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)).Value = userId;

                        // Parámetro de fecha
                        cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date)).Value = date;

                        // Si es asistente, pasamos el doctorUserId
                        if (doctorUserId.HasValue)
                        {
                            cmd.Parameters.Add(new SqlParameter("@DoctorUserId", SqlDbType.Int)).Value = doctorUserId.Value;
                        }

                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            availableHours.Add(reader["AvailableTime"].ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching available hours", ex);
                }
            }

            return availableHours;
        }

        /// <summary>
        /// CREAR UNA NUEVA CITA
        /// </summary>
        /// <param name="appointmentDto"></param>
        /// <param name="doctorUserId"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task CreateAppointmentAsync(Appointment appointmentDto, int? doctorUserId = null)
        {
            using (var connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_CreateAppointment", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Asignar parámetros al procedimiento almacenado
                    command.Parameters.AddWithValue("@appointment_createdate", DateTime.Now);
                    command.Parameters.AddWithValue("@appointment_modifydate", DateTime.Now);
                    command.Parameters.AddWithValue("@appointment_createuser", appointmentDto.AppointmentCreateuser);
                    command.Parameters.AddWithValue("@appointment_modifyuser", appointmentDto.AppointmentModifyuser);
                    command.Parameters.AddWithValue("@appointment_date", appointmentDto.AppointmentDate);
                    command.Parameters.AddWithValue("@appointment_hour", appointmentDto.AppointmentHour);
                    command.Parameters.AddWithValue("@appointment_patientid", appointmentDto.AppointmentPatientid);
                    command.Parameters.AddWithValue("@appointment_status", appointmentDto.AppointmentStatus);

                    // Validar si @doctor_userid es nulo antes de agregarlo
                    if (doctorUserId.HasValue)
                    {
                        command.Parameters.AddWithValue("@doctor_userid", doctorUserId.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@doctor_userid", DBNull.Value);
                    }

                    try
                    {
                        // Ejecutar el procedimiento almacenado y obtener el resultado
                        var result = await command.ExecuteScalarAsync();

                        // Deserializar el JSON retornado como un objeto en lugar de una lista
                        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result.ToString());

                        // Acceder a los valores del objeto deserializado
                        var success = jsonResponse.success;
                        var message = jsonResponse.message;
                        var appointmentId = jsonResponse.appointmentId;

                        if (success == 1)
                        {
                            Console.WriteLine($"Cita creada exitosamente con ID: {appointmentId}");
                        }
                        else
                        {
                            Console.WriteLine($"Error al crear la cita: {message}");
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Manejo de errores específicos
                        throw new ApplicationException("Error al crear la cita: " + ex.Message, ex);
                    }
                }
            }
        }


        /// <summary>
        /// Obtener cita por ID
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <param name="userProfile"></param>
        /// <returns></returns>
        public Appointment GetAppointmentById(int appointmentId, int userProfile)
        {
            Appointment appointment = null;

            using (SqlConnection connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                using (SqlCommand command = new SqlCommand("sp_GetAppointmentById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@appointment_id", appointmentId);
                    command.Parameters.AddWithValue("@UserProfile", userProfile);  // Pasando el perfil de usuario

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            appointment = new Appointment
                            {
                                AppointmentId = reader.GetInt32(reader.GetOrdinal("appointment_id")),
                                AppointmentCreatedate = reader.GetDateTime(reader.GetOrdinal("appointment_createdate")),
                                AppointmentModifydate = reader.GetDateTime(reader.GetOrdinal("appointment_modifydate")),
                                AppointmentCreateuser = reader.GetInt32(reader.GetOrdinal("appointment_createuser")),
                                AppointmentModifyuser = reader.GetInt32(reader.GetOrdinal("appointment_modifyuser")),
                                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                                AppointmentHour = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("appointment_hour"))),
                                AppointmentPatientid = reader.GetInt32(reader.GetOrdinal("appointment_patientid")),
                                AppointmentStatus = reader.GetInt32(reader.GetOrdinal("appointment_status"))
                            };

                            // Si el perfil es Asistente (3), agregar el DoctorUserId
                            if (userProfile == 3)
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("DoctorUserId")))
                                {
                                    appointment.DoctorUserId = reader.GetInt32(reader.GetOrdinal("DoctorUserId"));
                                }
                            }
                        }
                    }
                }
            }

            return appointment;
        }

        /// <summary>
        /// MODIFICAR UNA CITA
        /// </summary>
        /// <param name="appointmentDto"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task ModifyAppointmentAsync(Appointment appointmentDto)
        {
            using (var connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_UpdateAppointment", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros para el procedimiento almacenado
                    command.Parameters.AddWithValue("@appointment_id", appointmentDto.AppointmentId);
                    command.Parameters.AddWithValue("@appointment_modifydate", DateTime.Now);
                    command.Parameters.AddWithValue("@appointment_modifyuser", appointmentDto.AppointmentModifyuser);
                    command.Parameters.AddWithValue("@appointment_date", appointmentDto.AppointmentDate);
                    command.Parameters.AddWithValue("@appointment_hour", appointmentDto.AppointmentHour);
                    command.Parameters.AddWithValue("@appointment_patientid", appointmentDto.AppointmentPatientid);
                    command.Parameters.AddWithValue("@appointment_status", appointmentDto.AppointmentStatus);

                    try
                    {
                        // Ejecutar el procedimiento almacenado
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        // Manejo de errores, si ocurre algún problema al ejecutar el SP
                        throw new ApplicationException("Error al modificar la cita: " + ex.Message);
                    }
                }
            }
        }


        /// <summary>
        /// Cancelar una Cita
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <param name="modifiedBy"></param>
        public void DesactivateAppointment(int appointmentId, int modifiedBy)
        {
            using (SqlConnection connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                try
                {
                    connection.Open();

                    // Crear el comando para ejecutar el SP
                    using (SqlCommand cmd = new SqlCommand("sp_DesactiveAppointment", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Agregar parámetros
                        cmd.Parameters.Add(new SqlParameter("@AppointmentId", SqlDbType.Int)).Value = appointmentId;
                        cmd.Parameters.Add(new SqlParameter("@ModifiedBy", SqlDbType.Int)).Value = modifiedBy;

                        // Ejecutar el procedimiento almacenado
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores
                    Console.WriteLine($"Error al desactivar la cita: {ex.Message}");
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="createUserId"></param>
        /// <returns></returns>
        public async Task<List<AppointmentViewModel>> GetAppointmentsForTodayAsync(int createUserId)
        {
            var appointments = await _dbContext.Set<AppointmentViewModel>()
                 .FromSqlInterpolated($"EXEC sp_getAppointmentsForToday @CreateUser = {createUserId}")
                 .ToListAsync();

            return appointments;
        }

        //metodos para citas desde fuera
        /// <summary>
        /// 
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="appointmentDate"></param>
        /// <returns></returns>
        public Appointment GetAppointmentByPatientAndDay(int patientId, DateTime appointmentDate)
        {
            using (SqlConnection connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                using (SqlCommand command = new SqlCommand("sp_getAppointmentoByPatientandDay", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PatientID", patientId);
                    command.Parameters.AddWithValue("@AppointmentDate", appointmentDate);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Mapea los datos a tu modelo. Aquí se asume que la columna se llama "AppointmentID".
                            var appointment = new Appointment
                            {
                                AppointmentId = Convert.ToInt32(reader["appointment_id"])
                                // Mapea el resto de propiedades según necesites.
                            };
                            return appointment;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>

        public async Task<string> InsertVitalSignsAsync(VitalSignInputModel model)
        {
            try
            {
                var appointmentIdParam = new SqlParameter("@appointment_id", model.AppointmentId);
                var patientIdParam = new SqlParameter("@patient_id", model.PatientId);
                var temperatureParam = new SqlParameter("@temperature", model.Temperature);
                var respiratoryRateParam = new SqlParameter("@respiratory_rate", model.RespiratoryRate);
                var bpAsParam = new SqlParameter("@blood_pressureAS", model.BloodPressureAS ?? (object)DBNull.Value);
                var bpDisParam = new SqlParameter("@blood_pressureDIS", model.BloodPressureDIS ?? (object)DBNull.Value);
                var pulseParam = new SqlParameter("@pulse", model.Pulse ?? (object)DBNull.Value);
                var weightParam = new SqlParameter("@weight", model.Weight ?? (object)DBNull.Value);
                var sizeParam = new SqlParameter("@size", model.Size ?? (object)DBNull.Value);
                var createdByParam = new SqlParameter("@created_by", model.CreatedBy);

                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_InsertVitalSigns @appointment_id, @patient_id, @temperature, @respiratory_rate, " +
                    "@blood_pressureAS, @blood_pressureDIS, @pulse, @weight, @size, @created_by",
                    appointmentIdParam, patientIdParam, temperatureParam, respiratoryRateParam,
                    bpAsParam, bpDisParam, pulseParam, weightParam, sizeParam, createdByParam
                );

                return "Signos vitales insertados correctamente.";
            }
            catch (SqlException ex)
            {
                return $"Error SQL: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error general: {ex.Message}";
            }
        }

    }
}
