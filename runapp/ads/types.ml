type ad_format = Video | Carousel | Image

type ad = {
  id: string;
  format: ad_format;
  title: string;
  description: string;
  media_url: string;
  cta_text: string;
  cta_url: string;
  duration_sec: float option;  (* for video *)
  skippable_after_sec: float option;
  advertiser: string;
}

type ad_request = {
  user_id: string;
  slot: int;              (* 1 = feed, 2 = reels *)
  context: string list;    (* interests, etc *)
  device_type: string;
}

type ad_response = {
  ads: ad list;
  auction_id: string;
}