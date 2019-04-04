<template>
    <div class="apitest">
        <h2>
            Query: '{{ query }}', result:
            <div v-if="loading">Loading...</div>
            <div v-else>
                ID: {{ result.ID }}, Name: {{ result.firstName }}
                {{ result.lastName }}
            </div>
        </h2>
    </div>
</template>

<script>
import axios from "axios";
export default {
    name: "ApiTest",
    props: {
        query: String
    },
    data() {
        return {
            loading: true,
            result: null
        };
    },
    mounted() {
        axios
            .get("/api/" + this.query)
            .then(response => {
                this.result = response.data;
            })
            .finally(() => (this.loading = false));
    }
};
</script>
