﻿namespace MyTested.Mvc.Builders.Controllers
{
    using System;
    using System.Collections.Generic;
    using Contracts.Controllers;
    using Microsoft.AspNet.Mvc;
    using Microsoft.AspNet.Http;
    using Microsoft.AspNet.Http.Internal;
    using Utilities;
    using Contracts.Actions;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Actions;
    using Common;
    using Exceptions;
    using Common.Extensions;
    using Microsoft.AspNet.Mvc.Infrastructure;
    using Microsoft.Extensions.DependencyInjection;
    using Common.Identity;
    using System.Security.Claims;
    using Contracts;

    /// <summary>
    /// Used for building the action which will be tested.
    /// </summary>
    /// <typeparam name="TController">Class inheriting ASP.NET MVC 6 controller.</typeparam>
    public class ControllerBuilder<TController> : IAndControllerBuilder<TController>
        where TController : Controller // TODO: POCOController?
    {
        private readonly IDictionary<Type, object> aggregatedDependencies;

        private TController controller;
        private bool isPreparedForTesting;
        private bool enabledValidation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerBuilder{TController}" /> class.
        /// </summary>
        /// <param name="controllerInstance">Instance of the tested ASP.NET MVC 6 controller.</param>
        public ControllerBuilder(TController controllerInstance)
        {
            this.aggregatedDependencies = new Dictionary<Type, object>();

            this.Controller = controllerInstance;

            // TODO: for real this is how we configure controller?
            //var detailsProviders = new IMetadataDetailsProvider[]
            //{
            //    new DefaultBindingMetadataProvider(new ModelBindingMessageProvider
            //    {
            //        MissingBindRequiredValueAccessor = name => $"A value for the '{ name }' property was not provided.",
            //        MissingKeyOrValueAccessor = () => $"A value is required.",
            //        ValueMustNotBeNullAccessor = value => $"The value '{ value }' is invalid.",
            //    }),
            //    new DefaultValidationMetadataProvider(),
            //    new DataAnnotationsMetadataProvider(),
            //    // new DataMemberRequiredBindingMetadataProvider(), TODO: not available in version 8 but it is in the source code of MVC
            //};

            //var compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders);

            //var metadataProvider = new DefaultModelMetadataProvider(compositeDetailsProvider);
            //var httpContext = new DefaultHttpContext();
            //var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            //var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary());
            //var tempData = new TempDataDictionary(new HttpContextAccessor(), new SessionStateTempDataProvider());

            //var bindingContext = new ActionBindingContext()
            //{
            //    ModelBinder = new GenericModelBinder(),
            //    ValueProvider = new CompositeValueProvider(new IValueProvider[0]),
            //    InputFormatters = new List<IInputFormatter>(),
            //    ValidatorProvider = new DataAnnotationsModelValidatorProvider(
            //        options: null,
            //        stringLocalizerFactory: null)
            //};

            //this.Controller.ActionContext = actionContext;
            //this.Controller.BindingContext = bindingContext;
            //this.Controller.MetadataProvider = metadataProvider;
            //this.Controller.ViewData = viewData;
            //this.Controller.TempData = tempData;
            //this.Controller.ObjectValidator = new DefaultObjectValidator(new IExcludeTypeValidationFilter[0], metadataProvider);

            this.enabledValidation = true;
        }

        /// <summary>
        /// Gets the ASP.NET Web API controller instance to be tested.
        /// </summary>
        /// <value>Instance of the ASP.NET MVC 6 controller.</value>
        public TController Controller
        {
            get
            {
                this.BuildControllerIfNotExists();
                return this.controller;
            }

            private set
            {
                this.controller = value;
            }
        }

        public HttpRequest HttpRequest { get; private set; }

        public HttpContext HttpContext { get; private set; }

        /// <summary>
        /// Sets the HTTP context for the current test case.
        /// </summary>
        /// <param name="config">Instance of HttpContext.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithHttpContext(HttpContext context)
        {
            this.HttpContext = context; // TODO: is this needed? or set on the controller
            return this;
        }

        /// <summary>
        /// Adds HTTP request to the tested controller.
        /// </summary>
        /// <param name="requestMessage">Instance of HttpRequest.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithHttpRequest(HttpRequest request)
        {
            this.HttpRequest = request; // set it on the controller
            return this;
        }

        /// <summary>
        /// AndAlso method for better readability when building controller instance.
        /// </summary>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> AndAlso()
        {
            return this;
        }

        // TODO: HttpRequest builder

        /// <summary>
        /// Tries to resolve constructor dependency of given type.
        /// </summary>
        /// <typeparam name="TDependency">Type of dependency to resolve.</typeparam>
        /// <param name="dependency">Instance of dependency to inject into constructor.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithResolvedDependencyFor<TDependency>(TDependency dependency)
        {
            var typeOfDependency = dependency.GetType();
            if (this.aggregatedDependencies.ContainsKey(typeOfDependency))
            {
                throw new InvalidOperationException(string.Format(
                    "Dependency {0} is already registered for {1} controller.",
                    typeOfDependency.ToFriendlyTypeName(),
                    typeof(TController).ToFriendlyTypeName()));
            }

            this.aggregatedDependencies.Add(typeOfDependency, dependency);
            this.controller = null;
            return this;
        }

        /// <summary>
        /// Tries to resolve constructor dependencies by the provided collection of dependencies.
        /// </summary>
        /// <param name="dependencies">Collection of dependencies to inject into constructor.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithResolvedDependencies(IEnumerable<object> dependencies)
        {
            dependencies.ForEach(d => this.WithResolvedDependencyFor(d));
            return this;
        }

        /// <summary>
        /// Tries to resolve constructor dependencies by the provided dependencies.
        /// </summary>
        /// <param name="dependencies">Dependencies to inject into constructor.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithResolvedDependencies(params object[] dependencies)
        {
            dependencies.ForEach(d => this.WithResolvedDependencyFor(d));
            return this;
        }

        /// <summary>
        /// Disables ModelState validation for the action call.
        /// </summary>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithoutValidation()
        {
            this.enabledValidation = false;
            return this;
        }

        /// <summary>
        /// Sets default authenticated user to the built controller with "TestUser" username.
        /// </summary>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithAuthenticatedUser()
        {
            this.Controller.HttpContext.User = MockedClaimsPrincipal.CreateDefaultAuthenticated();
            return this;
        }

        /// <summary>
        /// Sets custom authenticated user using provided user builder.
        /// </summary>
        /// <param name="userBuilder">User builder to create mocked user object.</param>
        /// <returns>The same controller builder.</returns>
        public IAndControllerBuilder<TController> WithAuthenticatedUser(Action<IUserBuilder> userBuilder)
        {
            var newUserBuilder = new UserBuilder();
            userBuilder(newUserBuilder);
            this.Controller.HttpContext.User = newUserBuilder.GetUser();
            return this;
        }

        /// <summary>
        /// Used for testing controller attributes.
        /// </summary>
        /// <returns>Controller test builder.</returns>
        public IControllerTestBuilder ShouldHave()
        {
            var attributes = Reflection.GetCustomAttributes(this.Controller);
            return new ControllerTestBuilder(this.Controller, attributes);
        }

        /// <summary>
        /// Indicates which action should be invoked and tested.
        /// </summary>
        /// <typeparam name="TActionResult">Type of result from action.</typeparam>
        /// <param name="actionCall">Method call expression indicating invoked action.</param>
        /// <returns>Builder for testing the action result.</returns>
        public IActionResultTestBuilder<TActionResult> Calling<TActionResult>(Expression<Func<TController, TActionResult>> actionCall)
        {
            var actionInfo = this.GetAndValidateActionResult(actionCall);
            return new ActionResultTestBuilder<TActionResult>(
                this.Controller,
                actionInfo.ActionName,
                actionInfo.CaughtException,
                actionInfo.ActionResult,
                actionInfo.ActionAttributes);
        }

        /// <summary>
        /// Indicates which action should be invoked and tested.
        /// </summary>
        /// <typeparam name="TActionResult">Asynchronous Task result from action.</typeparam>
        /// <param name="actionCall">Method call expression indicating invoked action.</param>
        /// <returns>Builder for testing the action result.</returns>
        public IActionResultTestBuilder<TActionResult> CallingAsync<TActionResult>(Expression<Func<TController, Task<TActionResult>>> actionCall)
        {
            var actionInfo = this.GetAndValidateActionResult(actionCall);
            var actionResult = default(TActionResult);

            try
            {
                actionResult = actionInfo.ActionResult.Result;
            }
            catch (AggregateException aggregateException)
            {
                actionInfo.CaughtException = aggregateException;
            }

            return new ActionResultTestBuilder<TActionResult>(
                this.Controller,
                actionInfo.ActionName,
                actionInfo.CaughtException,
                actionResult,
                actionInfo.ActionAttributes);
        }

        /// <summary>
        /// Indicates which action should be invoked and tested.
        /// </summary>
        /// <param name="actionCall">Method call expression indicating invoked action.</param>
        /// <returns>Builder for testing void actions.</returns>
        public IVoidActionResultTestBuilder Calling(Expression<Action<TController>> actionCall)
        {
            var actionName = this.GetAndValidateAction(actionCall);
            var actionAttributes = ExpressionParser.GetMethodAttributes(actionCall);
            Exception caughtException = null;

            try
            {
                actionCall.Compile().Invoke(this.Controller);
            }
            catch (Exception exception)
            {
                caughtException = exception;
            }

            return new VoidActionResultTestBuilder(this.Controller, actionName, caughtException, actionAttributes);
        }

        /// <summary>
        /// Indicates which action should be invoked and tested.
        /// </summary>
        /// <param name="actionCall">Method call expression indicating invoked action.</param>
        /// <returns>Builder for testing void actions.</returns>
        public IVoidActionResultTestBuilder CallingAsync(Expression<Func<TController, Task>> actionCall)
        {
            var actionInfo = this.GetAndValidateActionResult(actionCall);

            try
            {
                actionInfo.ActionResult.Wait();
            }
            catch (AggregateException aggregateException)
            {
                actionInfo.CaughtException = aggregateException;
            }

            return new VoidActionResultTestBuilder(this.Controller, actionInfo.ActionName, actionInfo.CaughtException, actionInfo.ActionAttributes);
        }

        /// <summary>
        /// Gets ASP.NET MVC controller instance to be tested.
        /// </summary>
        /// <returns>Instance of the ASP.NET MVC controller.</returns>
        public TController AndProvideTheController()
        {
            return this.Controller;
        }

        /// <summary>
        /// Gets the HTTP configuration used in the testing.
        /// </summary>
        /// <returns>Instance of HttpConfiguration.</returns>
        public HttpRequest AndProvideTheHttpRequestMessage()
        {
            return this.HttpRequest;
        }

        // TODO: ?
        ///// <summary>
        ///// Gets the HTTP request message used in the testing.
        ///// </summary>
        ///// <returns>Instance of HttpRequestMessage.</returns>
        //public HttpConfiguration AndProvideTheHttpConfiguration()
        //{
        //    return this.Controller.Configuration;
        //}

        private void BuildControllerIfNotExists()
        {
            if (this.controller == null)
            {
                if (this.aggregatedDependencies.Any())
                {
                    this.controller = Reflection.TryCreateInstance<TController>(this.aggregatedDependencies.Select(v => v.Value).ToArray());
                }
                else
                {
                    this.controller = TestServiceProvider.TryCreateInstance<TController>();
                }

                if (this.controller == null)
                {
                    var friendlyDependenciesNames = this.aggregatedDependencies
                        .Keys
                        .Select(k => k.ToFriendlyTypeName());

                    var joinedFriendlyDependencies = string.Join(", ", friendlyDependenciesNames);

                    throw new UnresolvedDependenciesException(string.Format(
                        "{0} could not be instantiated because it contains no constructor taking {1} parameters.",
                        typeof(TController).ToFriendlyTypeName(),
                        this.aggregatedDependencies.Count == 0 ? "no" : string.Format("{0} as", joinedFriendlyDependencies)));
                }
            }

            if (!this.isPreparedForTesting)
            {
                this.PrepareController();
                this.isPreparedForTesting = true;
            }
        }

        private ActionInfo<TActionResult> GetAndValidateActionResult<TActionResult>(Expression<Func<TController, TActionResult>> actionCall)
        {
            var actionName = this.GetAndValidateAction(actionCall);
            var actionResult = default(TActionResult);
            var actionAttributes = ExpressionParser.GetMethodAttributes(actionCall);
            Exception caughtException = null;

            try
            {
                actionResult = actionCall.Compile().Invoke(this.Controller);
            }
            catch (Exception exception)
            {
                caughtException = exception;
            }

            return new ActionInfo<TActionResult>(actionName, actionAttributes, actionResult, caughtException);
        }

        private string GetAndValidateAction(LambdaExpression actionCall)
        {
            if (this.enabledValidation)
            {
                this.ValidateModelState(actionCall);
            }

            return ExpressionParser.GetMethodName(actionCall);
        }

        private void PrepareController()
        {
            this.controller.ActionContext = new ActionContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = MockedClaimsPrincipal.CreateUnauthenticated(),
                }
            };

            // TODO: add
            //this.controller.Request = this.HttpRequest;
            //this.controller.RequestContext = this.HttpRequestMessage.GetRequestContext();
            //this.controller.Configuration = this.HttpConfiguration ?? MyWebApi.Configuration ?? new HttpConfiguration();
        }

        private void ValidateModelState(LambdaExpression actionCall)
        {
            var arguments = ExpressionParser.ResolveMethodArguments(actionCall);
            foreach (var argument in arguments)
            {
                this.Controller.TryValidateModel(argument.Value);
            }
        }
    }
}
