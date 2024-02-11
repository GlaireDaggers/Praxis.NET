* {
	font: content/font/RussoOne-Regular.ttf;
	size: 32;
}

#scoretext {
    wordWrap: false;
    horizontalAlign: center;
}

.button {
    image: content/image/blue_panel.dds;
    slices: 10, 10, 10, 10;
    imageColor: 190, 190, 190, 255;
    textColor: 255, 255, 255, 255;
    horizontalAlign: center;
    verticalAlign: center;
}

.button:hover {
    imageColor: 255, 255, 255, 255;
    textColor: 255, 255, 0, 255;
}

.button:press {
    imageColor: 128, 128, 128, 255;
}

.input {
    image: content/image/blue_panel.dds;
    slices: 10, 10, 10, 10;
    verticalAlign: center;
}

.input-text {
    padding: 8, 4, 8, 4;
}