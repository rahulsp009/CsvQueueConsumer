using ModelsLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductServiceLibrary.Interfaces
{
    public interface IProductService
    {
        Task<int> AddProduct(Product product);
        Task UpdateFileProcessingStatus(int fileId);
    }
}
