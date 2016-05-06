/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("gulp-rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify");

var paths = {
    webroot: "./wwwroot/"
};

paths.bootstrapJs = paths.webroot + "js/bootstrap3-msgbox.js";
paths.formJs = paths.webroot + "js/form.js";
paths.logoutJs = paths.webroot + "js/logout.js";
paths.minJs = paths.webroot + "js/**/*.min.js";

paths.siteCss = paths.webroot + "css/site.css";
paths.formCss = paths.webroot + "css/form.css";
paths.menuCss = paths.webroot + "css/menu.css";
paths.minCss = paths.webroot + "css/**/*.min.css";

gulp.task("clean:js", function () {
    return gulp.src(paths.minJs, { read: false })
        .pipe(rimraf());
});

gulp.task("clean:css", function (cb) {
    return gulp.src(paths.minCss, { read: false })
       .pipe(rimraf());
});

gulp.task("clean", ["clean:js", "clean:css"]);

gulp.task("min:formJs", function () {
    return gulp.src([paths.bootstrapJs, paths.formJs], { base: "." })
        .pipe(concat(paths.webroot + "js/form.min.js"))
        .pipe(uglify())
        .pipe(gulp.dest("."));
});

gulp.task("min:logoutJs", function () {
    return gulp.src([paths.bootstrapJs, paths.logoutJs], { base: "." })
        .pipe(concat(paths.webroot + "js/logout.min.js"))
        .pipe(uglify())
        .pipe(gulp.dest("."));
});

gulp.task("min:js", ["min:formJs", "min:logoutJs"]);

gulp.task("min:skycss", function () {
    return gulp.src([paths.skyCss, "!" + paths.minCss])
        .pipe(concat(paths.concatSkyCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min:formCss", function () {
    return gulp.src([paths.siteCss, paths.formCss, paths.webroot + "css/form/sky-forms.css"])
        .pipe(concat(paths.webroot + "css/form.min.css"))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min:menuCss", function () {
    return gulp.src([paths.siteCss, paths.menuCss, paths.webroot + "css/menu/sky-mega-menu.css", paths.webroot + "css/tabs/sky-tabs.css"])
        .pipe(concat(paths.webroot + "css/menu.min.css"))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min:css", ["min:formCss", "min:menuCss"]);

gulp.task("min", ["min:js", "min:css"]);
