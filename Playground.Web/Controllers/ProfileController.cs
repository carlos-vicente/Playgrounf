﻿using System.Threading.Tasks;
using System.Web.Mvc;
using Playground.Web.Models;
using Playground.Web.Services.Contracts;

namespace Playground.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _service;

        public ProfileController(IUserService service)
        {
            _service = service;
        }

        public async Task<ActionResult> Index()
        {
            var profile = await _service
                .GetProfile("1")
                .ConfigureAwait(false);

            // TODO: automap Profile into ProfileView
            var viewModel = new ProfileViewModel();

            return View(viewModel);
        }
    }
}