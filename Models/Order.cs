using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public string Status { get; set; } = "Обработка";
        public string Address { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
