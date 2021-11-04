using CoinsManager.Web.Models;
using CoinsManager.Web.Models.View;
using CoinsManager.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoinsManager.Web.Controllers
{
    public class CoinCatalogController : Controller
    {
        private readonly ICoinCatalogService _coinCatalogService;

        public CoinCatalogController(ICoinCatalogService coinCatalogService)
        {
            _coinCatalogService = coinCatalogService;
        }

        public async Task<IActionResult> Index(int periodId)
        {
            ViewBag.Title = "Coins";

            var getCoins = _coinCatalogService.GetCoinsByPeriod(periodId);
            await Task.WhenAll(new Task[] { getCoins });

            return View(
                new CoinListModel { Coins = getCoins.Result });
        }

        public IActionResult Add()
        {
            ViewBag.Title = "Add Coin";
            return View(new Coin());
        }

        //[HttpPost]
        //public async Task<IActionResult> Add(Coin model)
        //{
        //    if (ModelState.IsValid)
        //        await _coinCatalogService.Add(model);

        //    return RedirectToAction("Index");
        //}
    }
}
