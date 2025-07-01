namespace ImageService.Core.Exceptions;

public class TargetHeightExceededException : Exception
{
    public TargetHeightExceededException() : base("Target height exceeds original.") { }
}
