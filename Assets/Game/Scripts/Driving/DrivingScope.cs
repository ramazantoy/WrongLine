using AF.Core;
using MessagePipe;
using VContainer;

namespace WrongLine.Driving
{
    /// <summary>The driving scene's scope. The road, the car and the camera register here as they arrive.</summary>
    public sealed class DrivingScope : SceneScope
    {
        protected override void ConfigureScene(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
        }
    }
}
