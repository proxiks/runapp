#include "reelplayer.h"
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QKeyEvent>
#include <QWheelEvent>
#include <QDir>

ReelPlayer::ReelPlayer(QWidget *parent) : QWidget(parent) {
    setupUI();
}

void ReelPlayer::setupUI() {
    auto* layout = new QVBoxLayout(this);
    layout->setContentsMargins(0, 0, 0, 0);
    layout->setSpacing(0);
    
    // Video
    player = new QMediaPlayer(this);
    videoWidget = new QVideoWidget(this);
    player->setVideoOutput(videoWidget);
    
    // Make video fill the widget
    videoWidget->setStyleSheet("background: black;");
    
    // Overlay
    overlay = new QWidget(videoWidget);
    overlay->setGeometry(0, height() - 200, width(), 200);
    overlay->setStyleSheet(
        "background: qlineargradient(x1:0, y1:0, x2:0, y2:1, "
        "stop:0 transparent, stop:1 rgba(0,0,0,0.8));"
    );
    
    auto* overlayLayout = new QVBoxLayout(overlay);
    
    userLabel = new QLabel("@jatin", overlay);
    userLabel->setStyleSheet("color: white; font-weight: 700; font-size: 16px;");
    
    captionLabel = new QLabel("Lyfron security in action 🔒", overlay);
    captionLabel->setStyleSheet("color: rgba(255,255,255,0.9); font-size: 14px;");
    captionLabel->setWordWrap(true);
    
    auto* actions = new QHBoxLayout();
    likeBtn = new QPushButton("♥ 1.2K", overlay);
    likeBtn->setStyleSheet(
        "background: rgba(255,255,255,0.15); color: white; "
        "border: none; border-radius: 20px; padding: 8px 16px;"
    );
    
    actions->addWidget(likeBtn);
    actions->addStretch();
    
    overlayLayout->addStretch();
    overlayLayout->addWidget(userLabel);
    overlayLayout->addWidget(captionLabel);
    overlayLayout->addLayout(actions);
    
    layout->addWidget(videoWidget);
    
    // Back button
    auto* backBtn = new QPushButton("← Back", this);
    backBtn->setStyleSheet(
        "position: absolute; top: 20px; left: 20px; "
        "background: rgba(0,0,0,0.5); color: white; "
        "border: none; border-radius: 8px; padding: 8px 16px;"
    );
    connect(backBtn, &QPushButton::clicked, this, &ReelPlayer::backToFeed);
    
    connect(player, &QMediaPlayer::mediaStatusChanged,
            this, &ReelPlayer::onMediaStatusChanged);
}

void ReelPlayer::loadReels() {
    // Load from user's videos directory
    reelUrls.clear();
    
    QDir videoDir(QDir::homePath() + "/RunApp/Videos");
    if (!videoDir.exists()) {
        videoDir.mkpath(".");
    }
    
    for (const auto& file : videoDir.entryInfoList({"*.mp4", "*.mov"})) {
        reelUrls.append(file.absoluteFilePath());
    }
    
    if (!reelUrls.isEmpty()) {
        loadReelAt(0);
    }
}

void ReelPlayer::loadReelAt(int index) {
    if (index < 0 || index >= reelUrls.size()) return;
    
    currentIndex = index;
    player->setMedia(QUrl::fromLocalFile(reelUrls[index]));
    player->play();
}

void ReelPlayer::playNext() {
    if (currentIndex < reelUrls.size() - 1) {
        loadReelAt(currentIndex + 1);
    }
}

void ReelPlayer::playPrev() {
    if (currentIndex > 0) {
        loadReelAt(currentIndex - 1);
    }
}

void ReelPlayer::onMediaStatusChanged(QMediaPlayer::MediaStatus status) {
    if (status == QMediaPlayer::EndOfMedia) {
        playNext();
    }
}

void ReelPlayer::keyPressEvent(QKeyEvent *event) {
    switch (event->key()) {
        case Qt::Key_Escape: emit backToFeed(); break;
        case Qt::Key_Down: playNext(); break;
        case Qt::Key_Up: playPrev(); break;
        case Qt::Key_Space:
            player->state() == QMediaPlayer::PlayingState 
                ? player->pause() : player->play();
            break;
    }
}

void ReelPlayer::wheelEvent(QWheelEvent *event) {
    if (event->angleDelta().y() < 0) {
        playNext();
    } else {
        playPrev();
    }
}