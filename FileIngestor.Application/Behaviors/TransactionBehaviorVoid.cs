using FileIngestor.Application.Interfaces;
using Common.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;



public class TransactionBehaviorVoid<TRequest> : IPipelineBehavior<TRequest, Unit>
    where TRequest : ICommandVoid 
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehaviorVoid<TRequest>> _logger;

    public TransactionBehaviorVoid(IUnitOfWork unitOfWork, ILogger<TransactionBehaviorVoid<TRequest>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(TRequest request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        var typeName = request.GetType().Name;

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