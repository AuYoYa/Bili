using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;
using Rougamo;
using Rougamo.Context;

namespace Ray.BiliBiliTool.Application.Attributes;

public class TaskInterceptorAttribute(
    string? taskName = null,
    TaskLevel taskLevel = TaskLevel.Two,
    bool rethrowWhenException = true
) : MoAttribute
{
    private readonly ILogger _logger = Global.ServiceProviderRoot!
        .GetRequiredService<ILogger<TaskInterceptorAttribute>>();

    // 修复：使用构造函数参数
    private readonly bool _rethrowWhenException = rethrowWhenException;

    public override void OnEntry(MethodContext context)
    {
        if (taskName is null)
            return;

        var delimiter = GetDelimiters();
        string end = taskLevel == TaskLevel.One ? Environment.NewLine : "";

        _logger.LogInformation($"{delimiter}开始 {taskName} {delimiter}{end}");
    }

    public override void OnExit(MethodContext context)
    {
        if (taskName is null)
            return;

        string delimiter = GetDelimiters();
        string append = new string(GetDelimiter(), taskName.Length);

        _logger.LogInformation($"{delimiter}{append}结束{append}{delimiter}{Environment.NewLine}");
    }

    public override void OnException(MethodContext context)
    {
        var ex = context.Exception ?? new Exception("Unknown exception");

        if (taskName is not null)
        {
            _logger.LogError(
                "{task}失败，继续其他任务。失败信息:{msg}{nl}",
                taskName,
                ex.Message,
                Environment.NewLine
            );
        }

        // 修复：避免 null returnValue 警告
        context.HandledException(this, ex);

        if (_rethrowWhenException)
        {
            throw ex;
        }
    }

    private string GetDelimiters()
    {
        int count = Convert.ToInt32(taskLevel.DefaultValue());
        return new string(GetDelimiter(), count);
    }

    private char GetDelimiter()
    {
        return taskLevel switch
        {
            TaskLevel.One => '=',
            TaskLevel.Two => '-',
            TaskLevel.Three => '*',
            _ => '-'
        };
    }
}
