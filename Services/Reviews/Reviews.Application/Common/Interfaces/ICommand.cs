using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Reviews.Application.Common.Interfaces;

/// <summary>Команда без результату.</summary>
public interface ICommand : IRequest { }

/// <summary>Команда з результатом (наприклад, Id створеного документа).</summary>
public interface ICommand<TResponse> : IRequest<TResponse> { }