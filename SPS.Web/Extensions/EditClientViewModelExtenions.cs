﻿using SPS.BO;
using SPS.Model;
using SPS.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SPS.Web.Extensions
{
    public static class EditClientViewModelExtenions
    {
        public static Client ToClient(this EditClientViewModel model, string passwordHash)
        {
            return new Client
            {
                Address = GetAddress(model),
                StreetNumber = int.Parse(model.Number),
                CPF = model.CPF,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Password = passwordHash,
                RG = model.RG,
                Telephone = model.PhoneNumber
            };
        }

        private static Address GetAddress(EditClientViewModel registerModel)
        {
            return new Address
            {
                City = registerModel.City,
                PostalCode = registerModel.PostalCode,
                Square = registerModel.Square,
                State = registerModel.State,
                Street = registerModel.Street
            };

        }
    }
}