using System.Collections.Generic;

namespace MyApp.Infraestructure.Data.Repository
{
    public interface IRepository<T> where T: class
    {
        IList<T> All();
    }
}