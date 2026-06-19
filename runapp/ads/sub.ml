type status = Active | Expired | Cancelled | GracePeriod

type t = {
  user_id : string;
  status : status;
  expires_at : Ptime.t;
  badge_visible : bool;
}

let is_verified t =
  match t.status with
  | Active | GracePeriod -> Ptime.is_later t.expires_at ~than:(Ptime_clock.now ())
  | _ -> false

let badge_svg () =
  (* Return SVG checkmark for app to render *)
  {| <svg viewBox="0 0 24 24" fill="#0d95ea"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg> |}