#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QStackedWidget>

class LoginWidget;
class FeedWidget;
class ReelPlayer;

class MainWindow : public QMainWindow {
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

public slots:
    void onLoginSuccess(const QString& userId, const QString& token);
    void onLogout();
    void showReels();

private:
    void setupUI();
    
    QStackedWidget* stack;
    LoginWidget* loginPage;
    FeedWidget* feedPage;
    ReelPlayer* reelPlayer;
};

#endif