using Assets.Scripts.Models;

namespace Assets.Scripts.Interfaces
{
    public interface IDiagramRenderer
    {
        void Render(DiagramModel diagram, object container);
        void Clear(object container);
    }
}
