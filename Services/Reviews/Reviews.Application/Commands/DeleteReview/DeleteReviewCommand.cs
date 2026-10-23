using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : ICommand;