﻿[<RequireQualifiedAccess>]
module Bot.Queue

open Azure.Storage.Queues
open Bot.Helpers
open FSharp
open Infrastructure.Settings
open Telegram.Core
open otsom.fs.Extensions

type File =
  | Link of url: string
  | Document of id: string * name: string

[<CLIMutable>]
type DownloaderMessage = { ConversionId: string; File: File }

let sendDownloaderMessage (workersSettings: WorkersSettings) logger =
  fun (message: DownloaderMessage) ->
    Logf.logfi logger "Sending queue message to downloader"
    let queueServiceClient = QueueServiceClient(workersSettings.ConnectionString)

    let queueClient =
      queueServiceClient.GetQueueClient(workersSettings.Downloader.Queue)

    let messageBody = JSON.serialize message

    queueClient.SendMessageAsync(messageBody) |> Task.ignore

[<CLIMutable>]
type ConverterMessage = { Id: string; Name: string }

let sendConverterMessage (workersSettings: WorkersSettings) =
  fun (message: ConverterMessage) ->
    let queueServiceClient = QueueServiceClient(workersSettings.ConnectionString)

    let queueClient =
      queueServiceClient.GetQueueClient(workersSettings.Converter.Input.Queue)

    let messageBody = JSON.serialize message

    queueClient.SendMessageAsync(messageBody) |> Task.ignore

type ConverterResultMessage =
  { Id: string; Result: ConversionResult }

let sendThumbnailerMessage (workersSettings: WorkersSettings) =
  fun (message: ConverterMessage) ->
    let queueServiceClient = QueueServiceClient(workersSettings.ConnectionString)

    let queueClient =
      queueServiceClient.GetQueueClient(workersSettings.Thumbnailer.Input.Queue)

    let messageBody = JSON.serialize message

    queueClient.SendMessageAsync(messageBody) |> Task.ignore

[<CLIMutable>]
type UploaderMessage = { ConversionId: string }

let queueUpload (workersSettings: WorkersSettings) : Domain.Workflows.Conversion.Completed.QueueUpload =
  fun conversion ->
    let queueServiceClient = QueueServiceClient(workersSettings.ConnectionString)

    let queueClient = queueServiceClient.GetQueueClient(workersSettings.Uploader.Queue)

    let messageBody = JSON.serialize { ConversionId = conversion.Id }

    queueClient.SendMessageAsync(messageBody) |> Task.ignore
