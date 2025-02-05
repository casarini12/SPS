﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SPS.Web.Models;
using SPS.Web.Extensions;
using SPS.BO;
using SPS.Model;
using Microsoft.AspNet.Identity.Owin;
using System.Net;
using System.Threading.Tasks;

namespace SPS.Web.Controllers
{
    public class ClientController : Controller
    {
        // GET: Client
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                var user = User.Identity.GetApplicationUser();

                if (user.UserType != Models.UserType.Client)
                {
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        public ActionResult Edit()
        {
            var user = User.Identity.GetApplicationUser();
            var client = BusinessManager.Instance.Clients.FindAll().SingleOrDefault(c => c.Email == user.Email);
            var model = client.ToEditClientViewModel();

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SaveEditChanges(EditClientViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                ApplicationUser user = userManager.FindByEmail(model.Email);
                bool error = false;

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var result = userManager.ChangePassword(user.Id, model.Password, model.NewPassword);

                    if (!result.Succeeded)
                    {
                        ModelState["Password"].Errors.Add("Senha incorreta");
                        error = true;
                    }
                }
                if (!error)
                {
                    user = userManager.FindByEmail(model.Email);

                    Client client = model.ToClient(user.PasswordHash);
                    BusinessManager.Instance.Clients.Update(client);

                    return RedirectToAction("Index", "Client");
                }
            }

            return View("Edit", model);
        }

        public ActionResult UsageRecords()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SaveRecord(SaveUsageRecordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = User.Identity.GetApplicationUser();
                var collaborator = BusinessManager.Instance.Collaborators.FindAll().SingleOrDefault(c => c.Email == user.Email);
                var tag = BusinessManager.Instance.Tags.Find(model.Tag);
                var client = BusinessManager.Instance.Clients.Find(tag.Client.CPF);
                var parking = BusinessManager.Instance.Parkings.Find(collaborator.Parking.CNPJ);
                bool isNew;

                BusinessManager.Instance.AddOrUpdateRecord(tag, parking, out isNew);
                return RedirectToAction("Index", "Collaborator");
            }

            return View(model);
        }
    }
}