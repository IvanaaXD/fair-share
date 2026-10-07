using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FairShare.Api.Filters
{
    /// <summary>
    /// Global filter that runs the FluentValidation validator registered for every action
    /// argument (request DTO) before the action executes. On failure it returns HTTP 400 with
    /// a ValidationProblemDetails body, so controllers never call validators themselves.
    ///
    /// This replaces FluentValidation.AspNetCore automatic validation, which the FluentValidation
    /// authors no longer recommend.
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null)
                    continue;

                // Look up IValidator<TheArgumentType>; arguments without a validator (Guid, int...) are skipped.
                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    context.HttpContext.RequestAborted);

                foreach (var failure in result.Errors)
                {
                    if (!errors.TryGetValue(failure.PropertyName, out var messages))
                        errors[failure.PropertyName] = messages = new List<string>();

                    messages.Add(failure.ErrorMessage);
                }
            }

            if (errors.Count > 0)
            {
                var problem = new ValidationProblemDetails(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Подаци у захтјеву нису исправни."
                };

                context.Result = new BadRequestObjectResult(problem);
                return;
            }

            await next();
        }
    }
}
