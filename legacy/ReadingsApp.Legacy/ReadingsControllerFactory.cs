using System;
using System.Web.Mvc;
using System.Web.Routing;
using ReadingsApp.Data;
using ReadingsApp.Legacy.Controllers;
using ReadingsApp.Services;

namespace ReadingsApp.Legacy
{
    /// <summary>
    /// Composition root. MVC 5 has no built-in dependency injection, so this is where the
    /// controller, service, repository and one DbContext per request are wired together.
    /// </summary>
    public class ReadingsControllerFactory : DefaultControllerFactory
    {
        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType != typeof(ReadingsController))
                return base.GetControllerInstance(requestContext, controllerType);

            var db = new ReadingsDbContext();
            requestContext.HttpContext.DisposeOnPipelineCompleted(db);
            return new ReadingsController(new ReadingService(new ReadingRepository(db)));
        }
    }
}
