﻿using Azure.Core;
using ExpertMed.Models;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace ExpertMed.Services
{
    public class UserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;
        private readonly DbExpertmedContext _dbContext;

        public UserService(IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger, DbExpertmedContext dbContext)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public List<Users> GetAllUsers(int usuarioId, int perfilId)
        {
            var users = new List<Users>();

            using (SqlConnection connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                using (SqlCommand command = new SqlCommand("sp_ListAllUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregar los parámetros al SP
                    command.Parameters.AddWithValue("@usuarioId", usuarioId);
                    command.Parameters.AddWithValue("@perfilId", perfilId);

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new Users
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("users_id")),
                                DocumentNumber = reader.GetString(reader.GetOrdinal("users_document_number")),
                                Names = reader.GetString(reader.GetOrdinal("users_names")),
                                Surnames = reader.GetString(reader.GetOrdinal("users_surcenames")),
                                Phone = reader.GetString(reader.GetOrdinal("users_phone")),
                                Email = reader.GetString(reader.GetOrdinal("users_email")),
                                CreationDate = reader.GetDateTime(reader.GetOrdinal("users_creationdate")),
                                ModificationDate = reader.GetDateTime(reader.GetOrdinal("users_modificationdate")),
                                Address = reader.GetString(reader.GetOrdinal("users_address")),
                                ProfilePhoto = reader["users_profilephoto"] as string,
                                ProfilePhoto64 = reader["users_profilephoto64"] as string,
                                SenecytCode = reader["users_senecytcode"] as string,
                                XKeyTaxo = reader["users_xkeytaxo"] as string,
                                XPassTaxo = reader["users_xpasstaxo"] as string,
                                Login = reader.GetString(reader.GetOrdinal("users_login")),
                                Password = reader.GetString(reader.GetOrdinal("users_password")),
                                Status = reader.GetInt32(reader.GetOrdinal("users_status")),
                                ProfileId = reader.GetInt32(reader.GetOrdinal("users_profileid")),
                                EstablishmentName = reader.GetString(reader.GetOrdinal("establishment_name")),
                                EstablishmentAddress = reader.GetString(reader.GetOrdinal("establishment_address")),
                                VatPercentageId = reader.GetInt32(reader.GetOrdinal("users_vatpercentageid")),
                                SpecialityId = reader.GetInt32(reader.GetOrdinal("users_specialityid")),
                                CountryId = reader.GetInt32(reader.GetOrdinal("users_countryid")),
                                Description = reader.GetString(reader.GetOrdinal("users_description")),
                                ProfileName = reader.GetString(reader.GetOrdinal("profile_name")),
                                SpecialityName = reader.GetString(reader.GetOrdinal("speciality_name")),
                                CountryName = reader.GetString(reader.GetOrdinal("country_name")),
                                VatBillingPercentage = reader.GetString(reader.GetOrdinal("vatbilling_percentage"))
                            };

                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }


        // Método para activar o desactivar al usuario
        public async Task<(bool success, string message)> DesactiveOrActiveUserAsync(int userId, int status)
        {
            try
            {
                // Crear la conexión
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    // Crear el comando para ejecutar el SP
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SP_DesactiveOrActiveUser";
                        command.CommandType = CommandType.StoredProcedure;

                        // Parámetros del procedimiento almacenado
                        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.Int) { Value = status });

                        // Ejecutar el comando y obtener la respuesta
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                string success = reader["success"].ToString();
                                string message = reader["message"].ToString();

                                if (success == "true")
                                {
                                    return (true, message);
                                }
                                else
                                {
                                    return (false, message);
                                }
                            }
                            else
                            {
                                return (false, "No se recibió respuesta válida del procedimiento.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar/desactivar el usuario.");
                return (false, "Ocurrió un error al procesar la solicitud.");
            }

        }

        //CREAR UN NUEVO USUARIO
        public async Task<int> CreateUserAsync(UserViewModel usuario, List<int>? associatedDoctorIds = null)
        {
            using (var connection = new SqlConnection(_dbContext.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand("sp_CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros para Datos Personales
                    command.Parameters.Add(new SqlParameter("@ProfilePhoto", SqlDbType.VarBinary)
                    {
                        Value = usuario.UserProfilephoto ?? (object)DBNull.Value
                    });
                    command.Parameters.AddWithValue("@ProfileId", usuario.UserProfileid);
                    command.Parameters.AddWithValue("@DocumentNumber", usuario.UserDocumentNumber);
                    command.Parameters.AddWithValue("@Names", usuario.UserNames);
                    command.Parameters.AddWithValue("@Surnames", usuario.UserSurnames);
                    command.Parameters.AddWithValue("@Address", usuario.UserAddress);
                    command.Parameters.AddWithValue("@SenecytCode", usuario.UserSenecytcode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Phone", usuario.UserPhone);
                    command.Parameters.AddWithValue("@Email", usuario.UserEmail);
                    command.Parameters.AddWithValue("@SpecialtyId", usuario.UserSpecialtyid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CountryId", usuario.UserCountryid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Login", usuario.UserLogin);
                    command.Parameters.AddWithValue("@Password", usuario.UserPassword);

                    // Parámetros para Taxo
                    command.Parameters.AddWithValue("@EstablishmentId", usuario.UserEstablishmentId ?? (object)DBNull.Value);
                   
                    command.Parameters.AddWithValue("@VatPercentageId", usuario.UserVatpercentageid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@XKeyTaxo", usuario.UserXkeytaxo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@XPassTaxo", usuario.UserXpasstaxo ?? (object)DBNull.Value);

                    // Parámetros para Horario y Descripción
                    command.Parameters.AddWithValue("@StartTime", usuario.StartTime == TimeOnly.MinValue
                        ? (object)DBNull.Value
                        : DateTime.Today.Add(usuario.StartTime.ToTimeSpan()));
                    command.Parameters.AddWithValue("@EndTime", usuario.EndTime == TimeOnly.MinValue
                        ? (object)DBNull.Value
                        : DateTime.Today.Add(usuario.EndTime.ToTimeSpan()));
                    command.Parameters.AddWithValue("@AppointmentInterval", usuario.AppointmentInterval);
                    command.Parameters.AddWithValue("@Description", usuario.UserDescription ?? (object)DBNull.Value);

                    // Parámetros para relaciones asistente-médico
                    if (associatedDoctorIds != null && associatedDoctorIds.Any())
                    {
                        string doctorIds = string.Join(",", associatedDoctorIds);
                        command.Parameters.AddWithValue("@DoctorIds", doctorIds);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@DoctorIds", DBNull.Value);
                    }

                    try
                    {
                        await connection.OpenAsync();

                        // Ejecutar y leer el resultado JSON
                        string jsonResult = null;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                jsonResult = reader.GetString(0);
                            }
                        }

                        if (string.IsNullOrEmpty(jsonResult))
                        {
                            throw new Exception("Error inesperado: No se obtuvo ningún resultado del procedimiento almacenado.");
                        }

                        // Deserializar el resultado JSON
                        using (JsonDocument document = JsonDocument.Parse(jsonResult))
                        {
                            var root = document.RootElement;

                            // Validar el resultado
                            if (root.TryGetProperty("success", out var success) && success.GetInt32() == 1)
                            {
                                if (root.TryGetProperty("userId", out var userId))
                                {
                                    return userId.GetInt32();
                                }
                                else
                                {
                                    throw new Exception("El campo 'userId' no se encuentra en el resultado.");
                                }
                            }
                            else
                            {
                                string errorMessage = root.TryGetProperty("message", out var message)
                                    ? message.GetString()
                                    : "Error al crear el usuario.";
                                throw new Exception(errorMessage);
                            }
                        }
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            await connection.CloseAsync();
                        }
                    }
                }
            }
        }

        //TRAER EL USUARIO POR EL ID
        // Método para obtener un usuario por su ID
        public async Task<UserWithDetails> GetUserDetailsAsync(int userId)
        {
            UserWithDetails userDetails = null;
            List<DoctorDto> doctors = new List<DoctorDto>();

            using (var connection = new SqlConnection(_dbContext.Database.GetConnectionString()))
            {
                try
                {
                    // Abrir la conexión
                    await connection.OpenAsync();

                    // Configurar el comando para ejecutar el procedimiento almacenado
                    using (var command = new SqlCommand("sp_ListUserById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@user_id", userId);

                        // Ejecutar el comando y leer los resultados
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Mapear los datos del usuario
                                userDetails = new UserWithDetails
                                {
                                    UserId = reader.GetInt32(reader.GetOrdinal("users_id")),
                                    DocumentNumber = reader.GetString(reader.GetOrdinal("users_document_number")),
                                    Names = reader.GetString(reader.GetOrdinal("users_names")),
                                    Surnames = reader.GetString(reader.GetOrdinal("users_surcenames")),
                                    Phone = reader.GetString(reader.GetOrdinal("users_phone")),
                                    Email = reader.GetString(reader.GetOrdinal("users_email")),
                                    CreationDate = reader.GetDateTime(reader.GetOrdinal("users_creationdate")),
                                    ModificationDate = reader.IsDBNull(reader.GetOrdinal("users_modificationdate"))
                                                       ? (DateTime?)null
                                                       : reader.GetDateTime(reader.GetOrdinal("users_modificationdate")),
                                    Address = reader.GetString(reader.GetOrdinal("users_address")),
                                    ProfilePhoto = reader["users_profilephoto"] != DBNull.Value ? (byte[])reader["users_profilephoto"] : null,
                                    ProfilePhoto64 = reader["users_profilephoto"] != DBNull.Value
                                        ? "data:image/png;base64," + Convert.ToBase64String((byte[])reader["users_profilephoto"])
                                        : "assets/images/users/UsersIcon", // Ruta por defecto
                                    SenecytCode = reader.IsDBNull(reader.GetOrdinal("users_senecytcode")) ? null : reader.GetString(reader.GetOrdinal("users_senecytcode")),
                                    XKeyTaxo = reader.IsDBNull(reader.GetOrdinal("users_xkeytaxo")) ? null : reader.GetString(reader.GetOrdinal("users_xkeytaxo")),
                                    XPassTaxo = reader.IsDBNull(reader.GetOrdinal("users_xpasstaxo")) ? null : reader.GetString(reader.GetOrdinal("users_xpasstaxo")),
                                    Login = reader.GetString(reader.GetOrdinal("users_login")),
                                    Status = reader.GetInt32(reader.GetOrdinal("users_status")),
                                    ProfileId = reader.GetInt32(reader.GetOrdinal("users_profileid")),
                                    UserCountryid = reader.GetInt32(reader.GetOrdinal("users_countryid")),
                                    UserDescription = reader.GetString(reader.GetOrdinal("users_description")) ?? "Sin especificar",
                                    ProfileName = reader.GetString(reader.GetOrdinal("profile_name")),
                                    UserEstablishmentid = reader.GetInt32(reader.GetOrdinal("user_establishment_id")),
                                  
                                    SpecialtyName = reader.GetString(reader.GetOrdinal("speciality_name")),
                                    CountryName = reader.GetString(reader.GetOrdinal("country_name")),
                                    StartTime = reader.GetTimeSpan(reader.GetOrdinal("start_time")),
                                    EndTime = reader.GetTimeSpan(reader.GetOrdinal("end_time")),
                                    AppointmentInterval = reader.GetInt32(reader.GetOrdinal("appointment_interval")),
                                    Doctors = doctors
                                };
                            }

                            // Si es un asistente (profile_id = 3), obtener los médicos relacionados
                            if (userDetails != null && userDetails.ProfileName == "Asistente")
                            {
                                // Reabrir el lector para obtener los médicos relacionados
                                if (await reader.NextResultAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        doctors.Add(new DoctorDto
                                        {
                                            DoctorId = reader.GetInt32(reader.GetOrdinal("doctor_id")),
                                            DoctorNames = reader.GetString(reader.GetOrdinal("doctor_names")),
                                            DoctorSurnames = reader.GetString(reader.GetOrdinal("doctor_surnames")),
                                            DoctorSpecialtyId = reader.GetInt32(reader.GetOrdinal("doctor_specialtyid")),
                                            DoctorSpecialtyName = reader.GetString(reader.GetOrdinal("doctor_specialty_name"))
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores, loguear el error si es necesario
                    throw new Exception("Error al obtener los detalles del usuario", ex);
                }
            }

            return userDetails;
        }

        //Metodo para actualizar un usuario


        public async Task UpdateUserAsync(int userId, UserViewModel usuario, List<int>? associatedDoctorIds = null)
        {
            using (var connection = new SqlConnection(_dbContext.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand("SP_UpdateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Agregar el parámetro del ID del usuario
                    command.Parameters.AddWithValue("@UserId", userId);

                    // Agregar los parámetros de datos personales
                    command.Parameters.Add(new SqlParameter("@ProfilePhoto", SqlDbType.VarBinary)
                    {
                        Value = usuario.UserProfilephoto ?? (object)DBNull.Value
                    });
                    command.Parameters.AddWithValue("@ProfileId", usuario.UserProfileid);
                    command.Parameters.AddWithValue("@DocumentNumber", usuario.UserDocumentNumber);
                    command.Parameters.AddWithValue("@Names", usuario.UserNames);
                    command.Parameters.AddWithValue("@Surnames", usuario.UserSurnames);
                    command.Parameters.AddWithValue("@Address", usuario.UserAddress);
                    command.Parameters.AddWithValue("@SenecytCode", usuario.UserSenecytcode ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Phone", usuario.UserPhone);
                    command.Parameters.AddWithValue("@Email", usuario.UserEmail);
                    command.Parameters.AddWithValue("@SpecialtyId", usuario.UserSpecialtyid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CountryId", usuario.UserCountryid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Login", usuario.UserLogin);
                    command.Parameters.AddWithValue("@Password", usuario.UserPassword);

                    // Parámetros para Taxo
                    command.Parameters.AddWithValue("@EstablishmentId ", usuario.UserEstablishmentId ?? (object)DBNull.Value);


                    command.Parameters.AddWithValue("@VatPercentageId", usuario.UserVatpercentageid ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@XKeyTaxo", usuario.UserXkeytaxo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@XPassTaxo", usuario.UserXpasstaxo ?? (object)DBNull.Value);

                    // Agregar los médicos asociados
                    if (associatedDoctorIds != null && associatedDoctorIds.Any())
                    {
                        string doctorIds = string.Join(",", associatedDoctorIds);
                        command.Parameters.AddWithValue("@DoctorIds", doctorIds);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@DoctorIds", DBNull.Value);
                    }

               
                    // Otros parámetros
                    command.Parameters.AddWithValue("@StartTime", usuario.StartTime == TimeOnly.MinValue ? DBNull.Value : DateTime.Today.Add(usuario.StartTime.ToTimeSpan()));
                    command.Parameters.AddWithValue("@EndTime", usuario.EndTime == TimeOnly.MinValue ? DBNull.Value : DateTime.Today.Add(usuario.EndTime.ToTimeSpan()));
                    command.Parameters.AddWithValue("@AppointmentInterval", usuario.AppointmentInterval);
                    command.Parameters.AddWithValue("@Description", usuario.UserDescription ?? (object)DBNull.Value);

                    try
                    {
                        await connection.OpenAsync();

                        // Ejecutar y leer el resultado JSON
                        string jsonResult = null;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                jsonResult = reader.GetString(0);
                            }
                        }

                        if (string.IsNullOrEmpty(jsonResult))
                        {
                            throw new Exception("Error inesperado: No se obtuvo ningún resultado del procedimiento almacenado.");
                        }

                        // Deserializar el resultado JSON
                        using (JsonDocument document = JsonDocument.Parse(jsonResult))
                        {
                            var root = document.RootElement;

                            // Validar el resultado
                            if (root.TryGetProperty("success", out var success) && success.GetInt32() == 1)
                            {
                                // Actualización exitosa
                                return;
                            }
                            else
                            {
                                string errorMessage = root.TryGetProperty("message", out var message)
                                    ? message.GetString()
                                    : "Error al actualizar el usuario.";
                                throw new Exception(errorMessage);
                            }
                        }
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            await connection.CloseAsync();
                        }
                    }
                }
            }
        }

    }
}
