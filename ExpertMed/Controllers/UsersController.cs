using ExpertMed.Models;
using ExpertMed.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpertMed.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly SelectsService _selectsService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserService userService, SelectsService selectsService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _selectsService = selectsService;
            _logger = logger;
        }

        public IActionResult UserList()
        {
            try
            {
                int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
                int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;
                List<Users> users = _userService.GetAllUsers(usuarioId, perfilId);
                return View(users);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Ocurrió un error al obtener la lista de usuarios.";
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserStatus(int userId, int status)
        {
            var result = await _userService.DesactiveOrActiveUserAsync(userId, status);
            TempData[result.success ? "SuccessMessage" : "ErrorMessage"] = result.message;
            return RedirectToAction("UserList");
        }

        [HttpGet]
        public async Task<IActionResult> NewUser()
        {
            try
            {
                int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
                int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

                var profiles = await _selectsService.GetAllProfilesAsync();
                var specialties = await _selectsService.GetAllSpecialtiesAsync();
                var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);
                var countries = await _selectsService.GetAllCountriesAsync();
                var percentage = await _selectsService.GetAllVatPercentageAsync();
                var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);

                var viewModel = new NewUserViewModel
                {
                    Profiles = profiles,
                    Specialties = specialties,
                    Users = medics,
                    Countries = countries,
                    VatBillings = percentage,
                    Establishments = establishments,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar datos para NewUser.");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> NewUser(UserViewModel usuario, IFormFile? ProfilePhoto, string? selectedDoctorIds)
        {
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Campo: {state.Key}, Error: {error.ErrorMessage}");
                    }
                }

                TempData["ErrorMessage"] = "Datos inválidos. Por favor, revisa los campos e intenta de nuevo.";
                int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
                int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

                var profiles = await _selectsService.GetAllProfilesAsync();
                var specialties = await _selectsService.GetAllSpecialtiesAsync();
                var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);
                var countries = await _selectsService.GetAllCountriesAsync();
                var percentage = await _selectsService.GetAllVatPercentageAsync();
                var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);

                var viewModel = new NewUserViewModel
                {
                    Profiles = profiles,
                    Specialties = specialties,
                    Users = medics,
                    Countries = countries,
                    VatBillings = percentage,
                    Establishments = establishments,
                };

                return View(viewModel);
            }

            usuario.UserProfilephoto = await ConvertFileToByteArray(ProfilePhoto);
            List<int>? associatedDoctorIds = selectedDoctorIds?.Split(',').Select(int.Parse).ToList();

            try
            {
                int idUsuarioCreado = await _userService.CreateUserAsync(usuario, associatedDoctorIds);
                TempData["SuccessMessage"] = "Usuario Creado Satisfactoriamente.";
                return RedirectToAction("UserList", "Users");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("El usuario con este CI ya existe."))
                    TempData["ErrorMessage"] = "El usuario con este CI ya existe.";
                else if (ex.Message.Contains("El nombre de usuario ya existe."))
                    TempData["ErrorMessage"] = "El nombre de usuario ya existe.";
                else
                    TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;

                int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
                int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

                var profiles = await _selectsService.GetAllProfilesAsync();
                var specialties = await _selectsService.GetAllSpecialtiesAsync();
                var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);
                var countries = await _selectsService.GetAllCountriesAsync();
                var percentage = await _selectsService.GetAllVatPercentageAsync();
                var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);

                var viewModel = new NewUserViewModel
                {
                    Profiles = profiles,
                    Specialties = specialties,
                    Users = medics,
                    Countries = countries,
                    VatBillings = percentage,
                    Establishments = establishments,
                };

                return View(viewModel);
            }
        }

        [HttpGet("Actualizar-Usuario")]
        public async Task<IActionResult> UpdateUser(int id)
        {
            var user = await _userService.GetUserDetailsAsync(id);
            if (user == null) return NotFound("User Not Found");

            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

            var profiles = await _selectsService.GetAllProfilesAsync();
            var specialties = await _selectsService.GetAllSpecialtiesAsync();
            var countries = await _selectsService.GetAllCountriesAsync();
            var percentage = await _selectsService.GetAllVatPercentageAsync();
            var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);
            var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);

            var viewModel = new NewUserViewModel
            {
                User = user,
                Profiles = profiles,
                Specialties = specialties,
                Countries = countries,
                VatBillings = percentage,
                Establishments = establishments,
                Users = medics,
                AssociatedDoctors = user.Doctors
            };

            return View(viewModel);
        }

        [HttpGet("Actualizar_Datos_Personales")]
        public async Task<IActionResult> UpdateUserP(int id)
        {
            var user = await _userService.GetUserDetailsAsync(id);
            if (user == null) return NotFound("User Not Found");

            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

            var profiles = await _selectsService.GetAllProfilesAsync();
            var specialties = await _selectsService.GetAllSpecialtiesAsync();
            var countries = await _selectsService.GetAllCountriesAsync();
            var percentage = await _selectsService.GetAllVatPercentageAsync();
            var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);
            var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);

            var viewModel = new NewUserViewModel
            {
                User = user,
                Profiles = profiles,
                Specialties = specialties,
                Countries = countries,
                VatBillings = percentage,
                Establishments = establishments,
                Users = medics,
                AssociatedDoctors = user.Doctors
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserMethod(UserViewModel usuario, IFormFile? ProfilePhoto, string? selectedDoctorIds, int id)
        {
            if (!ModelState.IsValid) return await ReloadUserEditView(id);

            usuario.UserProfilephoto = await ConvertFileToByteArray(ProfilePhoto);
            List<int>? associatedDoctorIds = selectedDoctorIds?.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();

            try
            {
                await _userService.UpdateUserAsync(id, usuario, associatedDoctorIds);
                TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                return RedirectToAction("UserList", "Users");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("El usuario no existe."))
                    TempData["ErrorMessage"] = "El usuario no existe.";
                else if (ex.Message.Contains("El número de documento ya existe."))
                    TempData["ErrorMessage"] = "El número de documento ya existe.";
                else if (ex.Message.Contains("El nombre de usuario ya está registrado."))
                    TempData["ErrorMessage"] = "El nombre de usuario ya está registrado.";
                else
                    TempData["ErrorMessage"] = "Error inesperado: " + ex.Message;

                return await ReloadUserEditView(id);
            }
        }

        private async Task<IActionResult> ReloadUserEditView(int id)
        {
            var user = await _userService.GetUserDetailsAsync(id);
            if (user == null) return NotFound("Usuario no encontrado.");

            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            int perfilId = HttpContext.Session.GetInt32("PerfilId") ?? 0;

            var profiles = await _selectsService.GetAllProfilesAsync();
            var specialties = await _selectsService.GetAllSpecialtiesAsync();
            var countries = await _selectsService.GetAllCountriesAsync();
            var percentage = await _selectsService.GetAllVatPercentageAsync();
            var establishments = await _selectsService.GetAllEstablishmentAsync(perfilId, usuarioId);
            var medics = await _selectsService.GetAllMedicsAsync(perfilId, usuarioId);

            var viewModel = new NewUserViewModel
            {
                User = user,
                Profiles = profiles,
                Specialties = specialties,
                Countries = countries,
                VatBillings = percentage,
                Establishments = establishments,
                Users = medics,
                AssociatedDoctors = user.Doctors
            };

            return View(viewModel);
        }

        public IActionResult ProfileSimple()
        {
            return View();
        }

        private async Task<byte[]> ConvertFileToByteArray(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            return null;
        }
    }
}
