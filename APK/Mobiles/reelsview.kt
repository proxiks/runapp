// In ReelsViewModel.kt
fun loadReels() {
    viewModelScope.launch {
        val reels = api.getReels()
        val reelsWithAds = mutableListOf<ReelsItem>()
        
        reels.forEachIndexed { index, reel ->
            reelsWithAds.add(ReelsItem.Content(reel))
            if ((index + 1) % 10 == 0) {
                val ad = api.getNextAd() // Returns null if verified user
                ad?.let { reelsWithAds.add(ReelsItem.Ad(it)) }
            }
        }
        _reels.value = reelsWithAds
    }
}