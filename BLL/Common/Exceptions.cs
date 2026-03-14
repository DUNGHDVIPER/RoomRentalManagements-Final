using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BLL.Common
{
    public static class Exceptions
    {
        // =========================
        // Error Code Enum
        // =========================
        public enum ErrorCode
        {
            Unknown = 0,
            NotFound = 1,
            Duplicate = 2,
            Validation = 3,
            BusinessRule = 4
        }

        // =========================
        // Base Exception
        // =========================
        [Serializable]
        public abstract class AppException : Exception
        {
            public ErrorCode Code { get; }

            protected AppException(ErrorCode code, string message)
                : base(message)
            {
                Code = code;
            }

            protected AppException(ErrorCode code, string message, Exception innerException)
                : base(message, innerException)
            {
                Code = code;
            }

            protected AppException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        // =========================
        // Business Exception
        // =========================
        [Serializable]
        public class BusinessException : AppException
        {
            public BusinessException(string message)
                : base(ErrorCode.BusinessRule, message)
            {
            }
        }

        // =========================
        // Not Found Exception
        // =========================
        [Serializable]
        public class NotFoundException : AppException
        {
            public NotFoundException(string message)
                : base(ErrorCode.NotFound, message)
            {
            }
        }

        // =========================
        // Duplicate Exception
        // =========================
        [Serializable]
        public class DuplicateException : AppException
        {
            public string FieldName { get; }

            public DuplicateException(string fieldName, string message)
                : base(ErrorCode.Duplicate, message)
            {
                FieldName = fieldName;
            }
        }

        // =========================
        // Validation Exception
        // =========================
        [Serializable]
        public class ValidationException : AppException
        {
            public Dictionary<string, string[]> Errors { get; }

            public ValidationException(Dictionary<string, string[]> errors)
                : base(ErrorCode.Validation, "One or more validation errors occurred.")
            {
                Errors = errors;
            }
        }
    }
}