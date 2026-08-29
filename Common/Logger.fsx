module Common.Logger

type LogLevel =
    | Debug
    | Info
    | Warning
    | Error

type Logger =
    { Message: string list
      MinLevel: LogLevel }


module Logger =
    open System

    let empty: Logger =
        { Message = []
          MinLevel = LogLevel.Info }

    let log level msg logger =
        let timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        let formatted = sprintf "[%s] [%s] %s" timestamp (string level) msg

        if level >= logger.MinLevel then
            { logger with
                Message = formatted :: logger.Message }
        else
            logger

    let merge logger1 logger2 =
        { Message = logger1.Message @ logger2.Message
          MinLevel = max logger1.MinLevel logger2.MinLevel }

    let toList logger = logger.Message |> List.rev

    let print logger =
        logger.Message |> List.rev |> List.iter (printfn "%s")

    let printWithLimit limit logger =
        logger.Message |> List.rev |> List.truncate limit |> List.iter (printfn "%s")
