using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Reviews.Application.Common.Interfaces;

/// <summary>Запит лише читає дані й нічого не змінює.</summary>
public interface IQuery<TResponse> : IRequest<TResponse> { }