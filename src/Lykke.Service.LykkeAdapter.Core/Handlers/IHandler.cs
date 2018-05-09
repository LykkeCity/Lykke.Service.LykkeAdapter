using System.Threading.Tasks;

namespace Lykke.Service.LykkeAdapter.Core.Handlers
{
    public interface IHandler<in T>
    {
        Task Handle(T message);
    }
}
