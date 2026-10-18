using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Bll.DTO;

public record TagDto(int TagId, string TagName);

public record CreateTagDto(string TagName);