<template>
  <div class="confirm-backdrop" @click.self="$emit('cancel')" @keydown.esc="$emit('cancel')">
    <div
      ref="dialog"
      class="confirm-dialog card border-0 shadow-lg"
      role="dialog"
      aria-modal="true"
      :aria-labelledby="titleId"
      :aria-describedby="messageId"
      tabindex="-1"
    >
      <div class="card-body p-4">
        <h2 :id="titleId" class="h5 mb-2">{{ title }}</h2>
        <p :id="messageId" class="text-muted mb-4">{{ message }}</p>
        <div class="d-flex justify-content-end">
          <button ref="cancelButton" type="button" class="btn btn-outline-secondary mr-2" @click="$emit('cancel')">
            {{ cancelLabel }}
          </button>
          <button type="button" class="btn btn-danger" :disabled="busy" @click="$emit('confirm')">
            {{ busy ? busyLabel : confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "ConfirmDialog",
  props: {
    title: { type: String, default: "Please confirm" },
    message: { type: String, default: "Are you sure you want to continue?" },
    confirmLabel: { type: String, default: "Confirm" },
    cancelLabel: { type: String, default: "Cancel" },
    busy: { type: Boolean, default: false },
    busyLabel: { type: String, default: "Working…" },
  },
  computed: {
    titleId() {
      return `confirm-title-${this._uid}`;
    },
    messageId() {
      return `confirm-message-${this._uid}`;
    },
  },
  mounted() {
    document.body.classList.add("modal-open");
    this.$nextTick(() => {
      if (this.$refs.cancelButton) this.$refs.cancelButton.focus();
    });
    window.addEventListener("keydown", this.onKeyDown);
  },
  beforeDestroy() {
    document.body.classList.remove("modal-open");
    window.removeEventListener("keydown", this.onKeyDown);
  },
  methods: {
    onKeyDown(event) {
      if (event.key === "Escape") this.$emit("cancel");
    },
  },
};
</script>

<style scoped>
.confirm-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  z-index: 2000;
}

.confirm-dialog {
  width: 100%;
  max-width: 440px;
  border-radius: 14px;
  outline: none;
}
</style>

<style>
body.modal-open {
  overflow: hidden;
}
</style>
