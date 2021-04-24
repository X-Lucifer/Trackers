using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;

namespace Trackers.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDistributedCache _cache;

        public IndexModel(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var content = await _cache.GetStringAsync("best_trackers") ?? "";
            return Content(content, "text/plain", Encoding.UTF8);
        }
    }
}
