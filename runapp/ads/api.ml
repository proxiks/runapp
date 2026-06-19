open Lwt.Infix

let meta_graph_url = "https://graph.facebook.com/v18.0"

let fetch_ads ~access_token ~ad_account_id () =
  let uri = Uri.of_string (meta_graph_url ^ "/" ^ ad_account_id ^ "/ads") in
  let uri = Uri.add_query_params uri [
    ("access_token", [access_token]);
    ("fields", ["name;creative{object_story_spec{photo_data,video_data}};status"]);
    ("effective_status", ["ACTIVE"]);
  ] in
  Cohttp_lwt_unix.Client.get uri >>= fun (_, body) ->
  Cohttp_lwt.Body.to_string body >|= fun json_str ->
  Yojson.Safe.from_string json_str
  |> parse_meta_ad_response
