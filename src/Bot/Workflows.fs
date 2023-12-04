﻿module Bot.Workflows

open Bot.Domain
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module UserConversion =
  type Load = string -> UserConversion Task
  type Save = UserConversion -> unit Task

[<RequireQualifiedAccess>]
module Conversion =
  [<RequireQualifiedAccess>]
  module New =
    type Load = string -> Conversion.New Task
    type Save = Conversion.New -> unit Task

  [<RequireQualifiedAccess>]
  module Prepared =
    type Load = string -> Conversion.Prepared Task
    type Save = Conversion.Prepared -> unit Task

  [<RequireQualifiedAccess>]
  module Converted =
    type Save = Conversion.Converted -> unit Task

  [<RequireQualifiedAccess>]
  module Thumbnailed =
    type Save = Conversion.Thumbnailed -> unit Task

  [<RequireQualifiedAccess>]
  module PreparedOrConverted =
    type Load = string -> Conversion.PreparedOrConverted Task

  [<RequireQualifiedAccess>]
  module PreparedOrThumbnailed =
    type Load = string -> Conversion.PreparedOrThumbnailed Task

  [<RequireQualifiedAccess>]
  module Completed =
    type Load = string -> Conversion.Completed Task
    type Save = Conversion.Completed -> unit Task