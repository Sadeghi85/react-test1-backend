using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application
{
    public class OperationResult<T>
    {
        private OperationResult(string reason) => FailureReason = reason;
        private OperationResult(T payload, int totalCount)
        {
            Payload = payload;
            TotalCount = totalCount;
        }

        public T? Payload { get; }
        public int TotalCount { get; }
        public string? FailureReason { get; }
        public bool IsSuccess => string.IsNullOrEmpty(FailureReason);

        public static OperationResult<T> Fail(string reason) => new OperationResult<T>(reason);
        public static OperationResult<T> Success(T payload, int totalCount) => new OperationResult<T>(payload, totalCount);
        public static implicit operator bool(OperationResult<T> result) => result.IsSuccess;
    }
}
