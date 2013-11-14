(function (angular, modernizr) {
    'use strict';

    var directives = angular.module('directives', []);

    //#region busyIndicator
    directives.directive('busyIndicator', ['$rootScope', function ($rootScope) {
        return {
            restrict: 'A',
            link: function (scope, elem) {
                var innerHtml = null;
                
                $rootScope.$on('event:ajax-show', function () {
                    if (innerHtml) return;
                    innerHtml = elem[0].innerHTML;
                    elem[0].innerHTML = 'Loading...';
                });
                
                $rootScope.$on('event:ajax-hide', function () {
                    elem[0].innerHTML = innerHtml || elem[0].innerHTML;
                    innerHtml = null;
                });
            }
        };
    }]);
    //#endregion

    //#region authDialog
    directives.directive('authDialog', function () {
        var instances = 0;
        
        return {
            restrict: 'A',
            link: function (scope, elem) {
                var $elem = $(elem);
                
                $elem.modal({
                    backdrop: false,
                    keyboard: false,
                    show: false
                });

                function showRequest() {
                    if (instances == 0) {
                        $elem.modal('show');
                    }
                    instances++;
                }
                
                function hideRequest() {
                    if (instances > 0) {
                        $elem.modal('hide');
                    }
                }

                function onHide() {
                    instances--;
                    if (instances > 0) {
                        instances = 0;
                        showRequest();
                    }
                }

                $(elem).on('hidden.bs.modal', onHide);
                scope.$on('event:auth-loginRequired', showRequest);
                scope.$on('event:auth-loginConfirmed', hideRequest);
                scope.$on('event:auth-loginCanceled', hideRequest);
            }
        };
    });
    //#endregion

    //#region Ng directives
    /*  We extend Angular with custom data bindings written as Ng directives */
    directives.directive('onFocus', function () {
        return {
            restrict: 'A',
            link: function (scope, elm, attrs) {
                elm.bind('focus', function () {
                    scope.$apply(attrs.onFocus);
                });
            }
        };
    })
        .directive('onBlur', function () {
            return {
                restrict: 'A',
                link: function (scope, elm, attrs) {
                    elm.bind('blur', function () {
                        scope.$apply(attrs.onBlur);
                    });
                }
            };
        })
        .directive('onEnter', function () {
            return function (scope, element, attrs) {
                element.bind("keydown keypress", function (event) {
                    if (event.which === 13) {
                        scope.$apply(function () {
                            scope.$eval(attrs.onEnter);
                        });

                        event.preventDefault();
                    }
                });
            };
        })
        .directive('selectedWhen', function () {
            return function (scope, elm, attrs) {
                scope.$watch(attrs.selectedWhen, function (shouldBeSelected) {
                    if (shouldBeSelected) {
                        elm.select();
                    }
                });
            };
        });
    if (!modernizr.input.placeholder) {
        // this browser does not support HTML5 placeholders
        // see http://stackoverflow.com/questions/14777841/angularjs-inputplaceholder-directive-breaking-with-ng-model
        directives.directive('placeholder', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                link: function (scope, element, attr, ctrl) {

                    var value;

                    var placeholder = function () {
                        element.val(attr.placeholder);
                    };
                    var unplaceholder = function () {
                        element.val('');
                    };

                    scope.$watch(attr.ngModel, function (val) {
                        value = val || '';
                    });

                    element.bind('focus', function () {
                        if (value == '') unplaceholder();
                    });

                    element.bind('blur', function () {
                        if (element.val() == '') placeholder();
                    });

                    ctrl.$formatters.unshift(function (val) {
                        if (!val) {
                            placeholder();
                            value = '';
                            return attr.placeholder;
                        }
                        return val;
                    });
                }
            };
        });
    }
    //#endregion 
    
})(window.angular, Modernizr);
