﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Owin;
using SPS.Web.Models;
using SPS.BO;
using SPS.Web.Services;
using System.Web.Script.Serialization;
using System.Net;
using SPS.Repository;
using SPS.Web.Extensions;
using SPS.Model;
using SPS.BO.Exceptions;
using SPS.Web.Common;

namespace SPS.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindAsync(model.Email, model.Password);

                if (user != null)
                {
                    if (user.UserType == UserType.Client && !user.EmailConfirmed)
                    {
                        ModelState.AddModelError("", "Email não confirmado. Por favor, verifique sua caixa de entrada e valide seu registro.");
                    }
                    else
                    {
                        if (user.UserType != UserType.GlobalAdmin)
                        {
                            await SignInAsync(user, model.RememberMe);
                        }
                        else
                        {
                            TempData["GlobalAdminEmail"] = user.Email;
                            TempData["GlobalAdminPassword"] = model.Password;
                        }

                        switch (user.UserType)
                        {
                            case UserType.Client:
                                return RedirectToAction("Index", "Client");
                            case UserType.Collaborator:
                                return RedirectToAction("Index", "Collaborator");
                            case UserType.LocalAdmin:
                                return RedirectToAction("Index", "LocalAdmin");
                            case UserType.GlobalAdmin:
                                return RedirectToAction("Login", "GlobalAdmin");
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Nome de usuário ou senha inválido.");
                }
            }

            // Se chegamos até aqui e houver alguma falha, exiba novamente o formulário
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = model.ToApplicationUser(UserType.Client);
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    Client client = model.ToUser<Client>(user.PasswordHash);

                    try
                    {
                        BusinessManager.Instance.Clients.Add(client);
                    }
                    catch (UniqueKeyViolationException ex)
                    {
                        ModelState["CPF"].Errors.Add(ex.Message);
                    }

                    // Enviar um email com este link
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                    await UserManager.SendEmailAsync(user.Id, "Confirmar sua conta Smart Parking System", "Olá, " + 
                        model.FirstName + "!<br/> Para começar a utilizar sua nova conta Smart Parking System,<br/> clique <a href=\"" + 
                        callbackUrl + "\">aqui</a>");

                    ViewBag.EmailSent = model.Email;

                    return View("ConfirmationSent");
                }
                else
                {
                    var emailErrors = result.Errors.Where(e => e.ToLower().Contains("email"));

                    if (emailErrors.Count() > 0)
                    {
                        foreach (var error in emailErrors)
                        {
                            ModelState["Email"].Errors.Add(string.Format("O email '{0}' já está cadastrado no sistema.", model.Email));
                        }
                    }
                }
            }

            // Se chegamos até aqui e houver alguma falha, exiba novamente o formulário
            return View(model);
        }

        public ActionResult RedirectUserToView()
        {
            var user = User.Identity.GetApplicationUser();

            switch (user.UserType)
            {
                case UserType.Client:
                    return RedirectToAction("Index", "Client");
                case UserType.Collaborator:
                    return RedirectToAction("Index", "Collaborator");
                case UserType.LocalAdmin:
                    return RedirectToAction("Index", "LocalAdmin");
                case UserType.GlobalAdmin:
                    return RedirectToAction("Index", "GlobalAdmin");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        //
        // GET: /Account/ConfirmationSent
        [AllowAnonymous]
        public ActionResult ConfirmationSent()
        {
            return View();
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }

            IdentityResult result = await UserManager.ConfirmEmailAsync(userId, code);

            if (result.Succeeded)
            {
                return View("ConfirmEmail");
            }
            else
            {
                AddErrors(result);
                return View();
            }
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);

                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    ModelState.AddModelError("", "O usuário não existe ou não está confirmado.");
                    return View();
                }

                // Para obter mais informações sobre como ativar a confirmação de senha e a redefinição de senha, visite http://go.microsoft.com/fwlink/?LinkID=320771
                // Enviar um email com este link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                await UserManager.SendEmailAsync(user.Id, "Redefinir senha", "Redefina sua senha clicando <a href=\"" + callbackUrl + "\">aqui</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // Se chegamos até aqui e houver alguma falha, exiba novamente o formulário
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            if (code == null)
            {
                return View("Error");
            }
            return View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Nenhum usuário encontrado.");
                    return View();
                }
                IdentityResult result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                else
                {
                    AddErrors(result);
                    return View();
                }
            }

            // Se chegamos até aqui, algo falhou, reexibir formulário
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));

            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                await SignInAsync(user, isPersistent: false);
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Sua senha foi alterada."
                : message == ManageMessageId.SetPasswordSuccess ? "Sua senha foi definida."
                : message == ManageMessageId.RemoveLoginSuccess ? "O logon externo foi removido."
                : message == ManageMessageId.Error ? "Ocorreu um erro."
                : "";

            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();

            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");

            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);

                    if (result.Succeeded)
                    {
                        var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

                        await SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // O usuário não tem uma senha, então remova os erros de validação causados por um campo OldPassword ausente
                ModelState state = ModelState["OldPassword"];

                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // Se chegamos até aqui, algo falhou, reexibir formulário
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

		//
		// POST: /Account/GetAddress
		[HttpPost]
		[AllowAnonymous]
		public ActionResult GetAddress(string postalCode)
		{
            var address = BusinessManager.Instance.Addresses.Find(postalCode); //SPSDb.Instance.Addresses.Find();

            if (address != null)
            {
                return Json(new JavaScriptSerializer().Serialize(address));
            }

            var postalService = new PostalCodeService();
            var result = postalService.GetAdrressFromPostalCode(postalCode);

            if (result.Address != null)
            {
                result.Address.State = StateTranslator.GetStateName(result.Address.State);
                BusinessManager.Instance.Addresses.Add(result.Address);
                //new AddressBO().Add(result.Address);
                return Json(new JavaScriptSerializer().Serialize(result.Address));
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, result.Message);
		}

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());

            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }

            base.Dispose(disposing);
        }

        #region Auxiliares
        // Usado para proteção XSRF ao adicionar logons externos
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());

            if (user != null)
            {
                return user.PasswordHash != null;
            }

            return false;
        }

        private void SendEmail(string email, string callbackUrl, string subject, string message)
        {

        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };

                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }

                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}