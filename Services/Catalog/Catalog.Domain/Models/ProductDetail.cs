using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain;

public class ProductDetail
{
    public int ProductId { get; set; }
    public string? Descrip { get; set; }
    public string OriginCountry { get; set; } = "";
    public string? Region { get; set; }
    public string TeaForm { get; set; } = "";
    public string TeaVariety { get; set; } = "";
    public string? FlavorNotes { get; set; }
    public int WeightGrams { get; set; }
}