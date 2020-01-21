module Assert

let equals expected actual =
    if expected <> actual then
        let error = sprintf "%A was expected but actual was %A" expected actual
        failwith error

let ok res =
    match res with
    | Ok _ -> ()
    | Error _ -> failwith "expected ok"
