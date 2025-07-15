using System.Data;

namespace OnigiriShop.Data.Interfaces
{
    public interface ISqliteConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
