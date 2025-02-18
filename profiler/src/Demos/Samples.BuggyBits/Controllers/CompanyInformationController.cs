// <copyright file="CompanyInformationController.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.
// </copyright>

using BuggyBits.Models;
using Microsoft.AspNetCore.Mvc;

namespace BuggyBits.Controllers
{
    public class CompanyInformationController : Controller
    {
        private readonly DataLayer dataLayer;

        public CompanyInformationController(DataLayer dataLayer)
        {
            this.dataLayer = dataLayer;
        }

        public IActionResult Index()
        {
            // bad blocking call
            ViewData["TessGithubPage"] = dataLayer.GetTessGithubPage().Result;

            return View();
        }

        [HttpPost]
        public IActionResult Contact(ContactViewModel model)
        {
            BuggyMail mail = new BuggyMail();
            mail.SendEmail(model.Message, "whocares-at-buggymail");
            return View("Index");
        }
    }
}
