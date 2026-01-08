using Common.Domain.Interfaces;
using FileIngestor.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileIngestor.Application.Behaviors
{
    public class TransactionBehaviorValue<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionBehaviorValue<TRequest, TResponse>> _logger;

        public TransactionBehaviorValue(IUnitOfWork unitOfWork, ILogger<TransactionBehaviorValue<TRequest, TResponse>> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var typeName = request.GetType().Name;

            var commandInterface = typeof(ICommand<>).MakeGenericType(typeof(TResponse));
            if (!commandInterface.IsAssignableFrom(request.GetType()))
            {
                return await next();
            }

            var txId = Guid.NewGuid();

            // Enriquecer contexto de logs para correlación
            using (LogContext.PushProperty("TransactionId", txId))
            using (LogContext.PushProperty("CommandName", typeName))
            {
                try
                {
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);
                    _logger.LogInformation(">>> TRx START: Transacción iniciada para el Command {CommandName}", typeName);

                    var response = await next();

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _logger.LogInformation("<<< TRx COMMIT: Transacción confirmada para el Command {CommandName}", typeName);

                    return response;
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(ex, "XXX TRx ROLLBACK: Transacción revertida para el Command {CommandName}. Mensaje: {ErrorMessage}", typeName, ex.Message);
                    throw;
                }
            }
        }
    }
}