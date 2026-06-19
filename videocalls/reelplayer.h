#ifndef REELPLAYER_H
#define REELPLAYER_H

#include <QWidget>
#include <QMediaPlayer>
#include <QVideoWidget>
#include <QStackedLayout>

class ReelPlayer : public QWidget {
    Q_OBJECT

public:
    explicit ReelPlayer(QWidget *parent = nullptr);
    void loadReels();

signals:
    void backToFeed();

protected:
    void keyPressEvent(QKeyEvent *event) override;
    void wheelEvent(QWheelEvent *event) override;

private slots:
    void playNext();
    void playPrev();
    void onMediaStatusChanged(QMediaPlayer::MediaStatus status);

private:
    void setupUI();
    void loadReelAt(int index);
    
    QMediaPlayer* player;
    QVideoWidget* videoWidget;
    QVector<QString> reelUrls;
    int currentIndex = 0;
    
    // Overlay UI
    QWidget* overlay;
    QLabel* userLabel;
    QLabel* captionLabel;
    QPushButton* likeBtn;
};

#endif