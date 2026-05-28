(module
  ;; import a tag from the host
  (import "test" "$import_tag" (tag $import_tag (param i32)))

  ;; Define a tag
  (tag $export_tag (param i32))

  ;; export the tag
  (export "$export_tag" (tag $export_tag))
)