﻿namespace Domain

open System.Threading.Tasks
open otsom.fs.Telegram.Bot.Core


module Core =
  type User = { Id: UserId; Lang: string option }

  type ConversionId = ConversionId of string

  [<RequireQualifiedAccess>]
  module Conversion =
    type Prepared = { Id: string; InputFile: string }
    type Converted = { Id: string; OutputFile: string }
    type Thumbnailed = { Id: string; ThumbnailName: string }

    type PreparedOrConverted = Choice<Prepared, Converted>
    type PreparedOrThumbnailed = Choice<Prepared, Thumbnailed>

    type Completed =
      { Id: string
        OutputFile: string
        ThumbnailFile: string }

    [<RequireQualifiedAccess>]
    module Prepared =
      type SaveThumbnail = Prepared -> string -> Task<Thumbnailed>
      type SaveVideo = Prepared -> string -> Task<Converted>

    [<RequireQualifiedAccess>]
    module Converted =
      type Complete = Converted -> string -> Task<Completed>

    [<RequireQualifiedAccess>]
    module Thumbnailed =
      type Complete = Thumbnailed -> string -> Task<Completed>