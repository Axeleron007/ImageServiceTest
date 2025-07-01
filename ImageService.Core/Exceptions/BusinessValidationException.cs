namespace ImageService.Core.Exceptions;

public class BusinessValidationException(string message) : Exception(message) { }