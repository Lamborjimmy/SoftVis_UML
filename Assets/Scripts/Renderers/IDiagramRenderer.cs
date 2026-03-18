using Assets.Scripts.Models;

namespace Assets.Scripts.Renderers
{
    public interface IDiagramRenderer
    {
        void Render(DiagramModel diagram, object container);
        void Clear(object container);
    }
}
