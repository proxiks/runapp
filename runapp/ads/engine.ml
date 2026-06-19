let select_ads (req: ad_request) : ad list =
  let candidates = match req.slot with
    | 1 -> Cache.get_feed_ads ()        (* standard feed *)
    | 2 -> Cache.get_reels_ads ()       (* vertical video format *)
    | _ -> []
  in
  
  (* Target by user context *)
  candidates
  |> List.filter (fun ad -> 
      List.exists (fun ctx -> 
        String.contains (String.lowercase_ascii ad.description) ctx
      ) req.context
    )
  |> List.sort (fun a b -> 
      (* Simple eCPM auction *)
      Float.compare b.bid_cpm a.bid_cpm
    )
  |> List.take 1  (* one ad per slot *)