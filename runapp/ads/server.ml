open Opium

let handle_ad_request req =
  let+ body = Request.to_json_exn req in
  let ad_req = parse_ad_request body in
  let ads = Ad_engine.select_ads ad_req in
  Response.of_json (ad_response_to_yojson { ads; auction_id = generate_id () })

let () =
  App.empty
  |> App.post "/v1/ads" handle_ad_request
  |> App.run_command