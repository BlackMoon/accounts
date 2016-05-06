/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
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
paths.skyJs = paths.webroot + "js/sky-*-ie8.js";

paths.concatFormJsDest = paths.webroot + "js/form.min.js";
paths.concatLogoutJsDest = paths.webroot + "js/logout.min.js";

paths.css = paths.webroot + "css/**/*.css";
paths.skyCss = paths.webroot + "css/**/sky-forms.css";
paths.minCss = paths.webroot + "css/**/*.min.css";

paths.concatCssDest = paths.webroot + "css/site2.min.css";
paths.concatSkyCssDest = paths.webroot + "css/sky-forms.min.css";

gulp.task("clean:js", function (cb) {
    rimraf(paths.minJs, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.concatCssDest, cb);
});

gulp.task("clean:skycss", function (cb) {
    rimraf(paths.concatSkyCssDest, cb);
});

gulp.task("clean", ["clean:js", "clean:css", "clean:skycss"]);

gulp.task("min:formJs", function () {
    return gulp.src([paths.bootstrapJs, paths.formJs], { base: "." })
        .pipe(concat(paths.concatFormJsDest))
        .pipe(uglify())
        .pipe(gulp.dest("."));
});

gulp.task("min:skycss", function () {
    return gulp.src([paths.skyCss, "!" + paths.minCss])
        .pipe(concat(paths.concatSkyCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.skyCss, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min", ["min:js", "min:css", "min:skycss"]);
