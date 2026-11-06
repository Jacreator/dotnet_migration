using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ReadingsApp.Data;

namespace ReadingsApp.Legacy
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<ReadingsDbContext, ReadingsMigrationsConfiguration>(true));

            ControllerBuilder.Current.SetControllerFactory(new ReadingsControllerFactory());

            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Readings", action = "Index", id = UrlParameter.Optional });
        }
    }
}
