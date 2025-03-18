using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // Verifica se a operação foi cancelada
        cancellationToken.ThrowIfCancellationRequested();

        // Executa a operação assíncrona
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = _inner.GetType()
            .GetMethod(nameof(IQueryProvider.Execute), new[] { typeof(Expression) })
            .MakeGenericMethod(resultType)
            .Invoke(_inner, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult });
    }

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        // Verifica se a expressão é válida
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        // Converte a expressão para um IQueryable<TResult>
        var queryable = _inner.CreateQuery<TResult>(expression);

        // Retorna um IAsyncEnumerable<TResult>
        return new TestAsyncEnumerable<TResult>(queryable);
    }
}

