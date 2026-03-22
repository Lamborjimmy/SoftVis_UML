using Softviz.UML.Models;

namespace Softviz.UML.Interfaces
{
    public interface IDiagramRenderer
    {
        void Render(DiagramModel diagram, object container);
        void Clear(object container);
    }
}
