using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using AppExceptions = FileIngestor.Application.Exceptions;

namespace FileIngestor.Application.Behaviors
{
    /// <summary>
    /// Comportamiento del pipeline de MediatR que realiza validación automática
    /// de la solicitud (Command/Query) antes de que el Handler sea ejecutado.
    /// </summary>
    /// <typeparam name="TRequest">El tipo de solicitud.</typeparam>
    /// <typeparam name="TResponse">El tipo de respuesta.</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    throw new AppExceptions.ValidationException(failures);
                }
            }

            
            return await next();
        }
    }
}