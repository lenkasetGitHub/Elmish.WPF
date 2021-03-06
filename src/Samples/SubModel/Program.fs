﻿module Elmish.WPF.Samples.SubModel.Program

open System
open Elmish
open Elmish.WPF

module Counter =

  type Model =
    { Count: int
      StepSize: int }
  
  let init =
    { Count = 0
      StepSize = 1 }
  
  type Msg =
    | Increment
    | Decrement
    | SetStepSize of int
    | Reset
    
  let update msg m =
    match msg with
    | Increment -> { m with Count = m.Count + m.StepSize }
    | Decrement -> { m with Count = m.Count - m.StepSize }
    | SetStepSize x -> { m with StepSize = x }
    | Reset -> init
  
  let bindings () : Binding<Model, Msg> list = [
    "CounterValue" |> Binding.oneWay (fun m -> m.Count)
    "Increment" |> Binding.cmd Increment
    "Decrement" |> Binding.cmd Decrement
    "StepSize" |> Binding.twoWay(
      (fun m -> float m.StepSize),
      int >> SetStepSize)
    "Reset" |> Binding.cmdIf(Reset, (<>) init)
  ]


module Clock =

  type Model =
    { Time: DateTimeOffset
      UseUtc: bool }

  let init () =
    { Time = DateTimeOffset.Now
      UseUtc = false }

  let getTime m =
    if m.UseUtc then m.Time.UtcDateTime else m.Time.LocalDateTime

  type Msg =
    | Tick of DateTimeOffset
    | ToggleUtc

  let update msg m =
    match msg with
    | Tick t -> { m with Time = t }
    | ToggleUtc -> { m with UseUtc = not m.UseUtc }

  let bindings () : Binding<Model, Msg> list = [
    "Time" |> Binding.oneWay getTime
    "ToggleUtc" |> Binding.cmd ToggleUtc
  ]


module CounterWithClock =

  type Model =
    { Counter: Counter.Model
      Clock: Clock.Model }

  let init =
    { Counter = Counter.init
      Clock = Clock.init () }

  type Msg =
    | CounterMsg of Counter.Msg
    | ClockMsg of Clock.Msg

  let update msg m =
    match msg with
    | CounterMsg msg -> { m with Counter = Counter.update msg m.Counter }
    | ClockMsg msg -> { m with Clock = Clock.update msg m.Clock }

  let bindings () : Binding<Model, Msg> list = [
    "Counter" |> Binding.subModel((fun m -> m.Counter), snd, CounterMsg, Counter.bindings)
    "Clock" |> Binding.subModel((fun m -> m.Clock), snd, ClockMsg, Clock.bindings)
  ]


module App =

  type Model =
    { ClockCounter1: CounterWithClock.Model
      ClockCounter2: CounterWithClock.Model }

  let init () =
    { ClockCounter1 = CounterWithClock.init
      ClockCounter2 = CounterWithClock.init }

  type Msg =
    | ClockCounter1Msg of CounterWithClock.Msg
    | ClockCounter2Msg of CounterWithClock.Msg

  let update msg m =
    match msg with
    | ClockCounter1Msg msg ->
        { m with ClockCounter1 = CounterWithClock.update msg m.ClockCounter1 }
    | ClockCounter2Msg msg ->
        { m with ClockCounter2 = CounterWithClock.update msg m.ClockCounter2 }

  let bindings () : Binding<Model, Msg> list = [
    "ClockCounter1" |> Binding.subModel(
      (fun m -> m.ClockCounter1),
      snd,
      ClockCounter1Msg,
      CounterWithClock.bindings)

    "ClockCounter2" |> Binding.subModel(
      (fun m -> m.ClockCounter2),
      snd,
      ClockCounter2Msg,
      CounterWithClock.bindings)
  ]


let timerTick dispatch =
  let timer = new System.Timers.Timer(1000.)
  timer.Elapsed.Add (fun _ ->
    let clockMsg =
      DateTimeOffset.Now
      |> Clock.Tick
      |> CounterWithClock.ClockMsg
    dispatch <| App.ClockCounter1Msg clockMsg
    dispatch <| App.ClockCounter2Msg clockMsg
  )
  timer.Start()


[<EntryPoint; STAThread>]
let main _ =
  Program.mkSimpleWpf App.init App.update App.bindings
  |> Program.withSubscription (fun _ -> Cmd.ofSub timerTick)
  |> Program.withConsoleTrace
  |> Program.runWindowWithConfig
    { ElmConfig.Default with LogConsole = true; Measure = true }
    (MainWindow())
