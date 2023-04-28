/**
 * @ngdoc controller
 * @name Umbraco.Editors.BlockList.BlockConfigurationController
 * @function
 *
 * @description
 * The controller for the content type editor property settings dialog
 */

(function () {
    "use strict";

    function TransferProperties(fromObject, toObject) {
        for (var p in fromObject) {
            toObject[p] = fromObject[p];
        }
    }

    function BlockConfigurationController($scope, elementTypeResource, overlayService, localizationService, editorService, eventsService, udiService) {

        var unsubscribe = [];

        const vm = this;
        vm.openBlock = null;

        function onInit() {

            if (!$scope.model.value) {
                $scope.model.value = [];
            }

            loadElementTypes();
        }

        function loadElementTypes() {
            return elementTypeResource.getAll().then(elementTypes => {
                vm.elementTypes = elementTypes;
            });
        }

        function updateUsedElementTypes(event, args) {
            var key = args.documentType.key;
            for (var i = 0; i < vm.elementTypes.length; i++) {
                if (vm.elementTypes[i].key === key) {
                    vm.elementTypes[i] = args.documentType;
                }
            }
        }

        unsubscribe.push(eventsService.on("editors.documentType.saved", updateUsedElementTypes));

        vm.requestRemoveBlockByIndex = function (index, event) {

            const labelKeys = [
                "general_delete",
                "blockEditor_confirmDeleteBlockTypeMessage",
                "blockEditor_confirmDeleteBlockTypeNotice"
            ];

            localizationService.localizeMany(labelKeys).then(data => {
                var contentElementType = vm.getElementTypeByKey($scope.model.value[index].contentElementTypeKey);
                overlayService.confirmDelete({
                    title: data[0],
                    content: localizationService.tokenReplace(data[1], [contentElementType ? contentElementType.name : "(Unavailable ElementType)"]),
                    confirmMessage: data[2],
                    submit: () => {
                        vm.removeBlockByIndex(index);
                        overlayService.close();
                    },
                    close: overlayService.close()
                });
            });

            event.stopPropagation();
        };

        vm.removeBlockByIndex = function (index) {
            $scope.model.value.splice(index, 1);
        };

        vm.sortableOptions = {
            "ui-floating": true,
            items: "umb-block-card",
            cursor: "grabbing",
            placeholder: 'umb-block-card --sortable-placeholder'
        };

        vm.getAvailableElementTypes = function () {
            return vm.elementTypes.filter(function (type) {
                return !$scope.model.value.find(function (entry) {
                    return type.key === entry.contentElementTypeKey;
                });
            });
        };

        vm.getElementTypeByKey = function (key) {
            if (vm.elementTypes) {
                return vm.elementTypes.find(type => type.key === key) || null;
            }
        };

        vm.openAddDialog = function () {

            localizationService.localize("blockEditor_headlineCreateBlock").then(localizedTitle => {

                const contentTypePicker = {
                    title: localizedTitle,
                    section: "settings",
                    treeAlias: "documentTypes",
                    entityType: "documentType",
                    isDialog: true,
                    filter: function (node) {
                        if (node.metaData.isElement === true) {
                            var key = udiService.getKey(node.udi);

                            // If a Block with this ElementType as content already exists, we will emit it as a posible option.
                            return $scope.model.value.find(entry => entry.contentElementTypeKey === key);
                        }
                        return true;
                    },
                    filterCssClass: "not-allowed",
                    select: function (node) {
                        vm.addBlockFromElementTypeKey(udiService.getKey(node.udi));
                        editorService.close();
                    },
                    close: function () {
                        editorService.close();
                    },
                    extraActions: [
                        {
                            style: "primary",
                            labelKey: "blockEditor_labelcreateNewElementType",
                            action: function () {
                                vm.createElementTypeAndCallback((documentTypeKey) => {
                                    vm.addBlockFromElementTypeKey(documentTypeKey);

                                    // At this point we will close the contentTypePicker.
                                    editorService.close();
                                });
                            }
                        }
                    ]
                };

                editorService.treePicker(contentTypePicker);
            });
        };

        vm.createElementTypeAndCallback = function (callback) {
            const editor = {
                create: true,
                infiniteMode: true,
                noTemplate: true,
                isElement: true,
                submit: function (model) {
                    loadElementTypes().then(() => {
                        callback(model.documentTypeKey);
                    });

                    editorService.close();
                },
                close: function () {
                    editorService.close();
                }
            };
            editorService.documentTypeEditor(editor);
        }

        vm.addBlockFromElementTypeKey = function (key) {

            const blockType = {
                contentElementTypeKey: key,
                settingsElementTypeKey: null,
            };

            $scope.model.value.push(blockType);

        };

        $scope.$on('$destroy', function () {
            unsubscribe.forEach(u => { u(); });
        });

        onInit();

    }

    angular.module("umbraco").controller("Yuzu.PropertyEditors.BlockList.BlockConfigurationController", BlockConfigurationController);

})();
