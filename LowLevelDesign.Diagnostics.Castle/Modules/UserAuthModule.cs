﻿using LowLevelDesign.Diagnostics.Castle.Config;
using LowLevelDesign.Diagnostics.Castle.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LowLevelDesign.Diagnostics.Castle.Modules
{
    public class UserProfileModule : NancyModule
    {
        public UserProfileModule()
        {
            this.RequiresAuthentication();

            Get["/auth/passwd"] = _ => {
                return View["Auth/ChangePassword.cshtml", new ResetPasswordViewModel()];
            };

            Post["/auth/passwd", true] = async (x, ct) => {
                var model = this.BindAndValidate<ResetPasswordViewModel>();
                if (!String.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal)) {
                    ModelValidationResult.Errors.Add("", "The password and confirmation password do not match.");
                }
                if (this.ModelValidationResult.IsValid) {
                    var u = await UserManager.FindByNameAsync(Context.CurrentUser.UserName);
                    var result = await UserManager.ChangePasswordAsync(u.Id, model.OldPassword, model.Password);
                    if (result == IdentityResult.Success) {
                        ViewBag.PasswordChanged = true;
                        return View["Auth/ChangePassword.cshtml", new ResetPasswordViewModel()];
                    }
                    ModelValidationResult.Errors.Add("", "Invalid login attempt");
                }
                ViewBag.ValidationErrors = ModelValidationResult;
                return View["Auth/ChangePassword.cshtml", model];
            };
        }
        public ApplicationSignInManager SignInManager
        {
            get { return Context.GetFromOwinContext<ApplicationSignInManager>(); }
        }

        public ApplicationUserManager UserManager
        {
            get { return Context.GetFromOwinContext<ApplicationUserManager>(); }
        }

        private String[] GetErrorsListFromValidationResult(ModelValidationResult result)
        {
            var errors = new List<String>(10);
            foreach (var err in result.Errors) {
                errors.AddRange(err.Value.Select(suberr => suberr.ErrorMessage));
            }
            return errors.ToArray();
        }
    }
}